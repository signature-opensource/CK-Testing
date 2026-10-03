using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using CK.Core;

namespace CK.Testing;


/// <summary>
/// Static part of the implementation of <see cref="MonitorTestHelper"/>: the paths and the build
/// configuration are computed once per process, before the helper exists.
/// </summary>
static partial class StaticTestHelper
{
    static readonly string[] _allowedConfigurations = new[] { "Debug", "Release" };
    internal static readonly NormalizedPath _binFolder;
    internal static readonly string _buildConfiguration;
    internal static readonly NormalizedPath _testProjectFolder;
    internal static readonly NormalizedPath _pathToBin;
    internal static readonly NormalizedPath _solutionFolder;
    internal static readonly string _solutionName;
    internal static readonly NormalizedPath _logFolder;
    internal static readonly HashSet<string> _onlyOnce;
    internal static readonly ExceptionDispatchInfo _initializationError;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    static StaticTestHelper()
    {
        _onlyOnce = new HashSet<string>();
        try
        {
            // Conservative approach here: we inject our own Listener if and only if it replaces the (only) default one.
            if( Trace.Listeners.Count == 1
                && Trace.Listeners[0] is DefaultTraceListener def
                && def.Name == "Default" )
            {
                Trace.Listeners.Clear();
                Trace.Listeners.Add( new SafeTraceListener() );
            }

            string? p = AppContext.BaseDirectory;
            _binFolder = p;
            string? buildConfDir = null;
            foreach( var config in _allowedConfigurations )
            {
                buildConfDir = FindAbove( p, config );
                if( buildConfDir != null )
                {
                    _buildConfiguration = config;
                    break;
                }
            }
            if( _buildConfiguration == null )
            {
                Throw.InvalidOperationException( $"Initialization error: Unable to find parent folder named '{_allowedConfigurations.Concatenate( "' or '" )}' above '{_binFolder}'." );
            }
            Throw.DebugAssert( buildConfDir != null );
            p = FindAbove( p, "bin" );
            if( p == null )
            {
                Throw.InvalidOperationException( $"Initialization error: Unable to find 'bin' folder above '{_binFolder}'." );
            }
            Throw.DebugAssert( p != null );
            p = Path.GetDirectoryName( p );
            if( string.IsNullOrEmpty( p ) )
            {
                Throw.InvalidOperationException( $"The '{_binFolder}' must not be directly on the root." );
            }
            _testProjectFolder = p;

            // The solution folder is the git working folder: a main checkout, a linked git worktree or a submodule.
            // In a linked worktree, the solution name is the name of the main repository.
            if( !LocalDevSolution.TryFindSolutionFolder( _binFolder, out var solutionFolder, out var solutionName, out _ ) )
            {
                Throw.InvalidOperationException( $"Initialization error: The project must be in a git repository (above '{_binFolder}')." );
            }
            if( IsRootFolder( solutionFolder ) )
            {
                Throw.InvalidOperationException( $"The '.git' cannot be directly on the root." );
            }
            if( solutionFolder.Parts.Count >= _testProjectFolder.Parts.Count || !_testProjectFolder.StartsWith( solutionFolder ) )
            {
                Throw.InvalidOperationException( $"Initialization error: The git working folder '{solutionFolder}' must be above the test project folder '{_testProjectFolder}'." );
            }
            if( FindTestsFolder( _testProjectFolder, solutionFolder ) == null )
            {
                Throw.InvalidOperationException( $"Initialization error: A parent 'Tests' folder must exist above '{_testProjectFolder}'." );
            }
            _solutionFolder = solutionFolder;
            _solutionName = solutionName;
            _logFolder = _testProjectFolder.AppendPart( "Logs" );
            _pathToBin = _binFolder.RemoveParts( 0, _testProjectFolder.Parts.Count );
        }
        catch( Exception ex )
        {
            _initializationError = ExceptionDispatchInfo.Capture( ex );
        }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    static string? FindAbove( string path, string folderName )
    {
        var p = path;
        while( p != null && Path.GetFileName( p ) != folderName )
        {
            p = Path.GetDirectoryName( p );
        }
        return p;
    }

    /// <summary>
    /// Gets whether a path is a root: the empty path, "/", a drive ("C:") or a UNC share ("//server/share").
    /// A rooted path with one part, like "/src", is not a root.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>True if the path is a root.</returns>
    internal static bool IsRootFolder( NormalizedPath path )
    {
        return path.Parts.Count == 0
               || (path.Parts.Count == 1 && path.RootKind == NormalizedPathRootKind.RootedByFirstPart)
               || (path.Parts.Count <= 2 && path.RootKind == NormalizedPathRootKind.RootedByDoubleSeparator);
    }

    /// <summary>
    /// Finds the deepest "Tests" folder above the test project folder.
    /// <para>
    /// The folders between the test project folder and the solution folder are tried first.
    /// A git submodule is its own solution, but it is in the working folder of its superproject, often in its
    /// "Tests" folder. When the solution is a submodule, the folders of the superproject above the submodule are
    /// tried too, up to the superproject folder. This repeats for nested submodules. Above a main checkout or a
    /// linked worktree, no folder is tried.
    /// </para>
    /// <para>
    /// The superproject is found with <see cref="LocalDevSolution.TryFindSolutionFolder(string, out NormalizedPath, out string?, out string?)"/>:
    /// a ".git" file that does not point to a git directory is ignored.
    /// </para>
    /// </summary>
    /// <param name="testProjectFolder">The test project folder.</param>
    /// <param name="solutionFolder">The solution folder, above the test project folder.</param>
    /// <returns>The "Tests" folder, or null if no "Tests" folder is found.</returns>
    internal static NormalizedPath? FindTestsFolder( NormalizedPath testProjectFolder, NormalizedPath solutionFolder )
    {
        var found = FindBelow( testProjectFolder.RemoveLastPart(), solutionFolder );
        var boundary = solutionFolder;
        while( found == null && IsSubmodule( boundary ) )
        {
            var parent = boundary.RemoveLastPart();
            if( !LocalDevSolution.TryFindSolutionFolder( parent, out var superproject, out _, out _ ) ) break;
            found = FindBelow( parent, superproject );
            boundary = superproject;
        }
        return found;

        static NormalizedPath? FindBelow( NormalizedPath start, NormalizedPath root )
        {
            for( var p = start; p.Parts.Count > root.Parts.Count; p = p.RemoveLastPart() )
            {
                if( p.LastPart == "Tests" ) return p;
            }
            return null;
        }

        // A submodule has a ".git" file and LocalDevSolution sees it as a solution folder without worktree identifier.
        // The search starts at the folder: the folder is the solution folder when the result has the same depth.
        static bool IsSubmodule( NormalizedPath folder )
        {
            return File.Exists( folder.AppendPart( ".git" ) )
                   && LocalDevSolution.TryFindSolutionFolder( folder, out var f, out _, out var worktreeId )
                   && f.Parts.Count == folder.Parts.Count
                   && worktreeId == null;
        }
    }

    /// <summary>
    /// Enumerates the <see cref="IMonitorTestHelper.ClosestSUTProjectFolder"/> candidate paths, starting with the best one.
    /// </summary>
    /// <param name="solutionFolder">The root folder: nothing happen above this one.</param>
    /// <param name="testProjectFolder">The test project that must be in <paramref name="solutionFolder"/> and contains at least one "Tests" part.</param>
    /// <returns>The closest SUT path in order of preference.</returns>
    internal static IEnumerable<NormalizedPath> GetClosestSUTProjectCandidatePaths( NormalizedPath solutionFolder, NormalizedPath testProjectFolder )
    {
        Throw.CheckArgument( testProjectFolder.StartsWith( solutionFolder ) );

        string? targetName = null;
        if( testProjectFolder.LastPart.EndsWith( ".Tests" ) ) targetName = testProjectFolder.LastPart.Substring( 0, testProjectFolder.LastPart.Length - 6 );
        if( !String.IsNullOrEmpty( targetName ) )
        {
            var cache = new List<NormalizedPath>();
            // The .SUT always has the priority, wherever it is.
            var sutTargetName = targetName + ".SUT";
            foreach( var p in GetClosestCandidates( solutionFolder.Parts.Count, testProjectFolder.RemoveLastPart(), sutTargetName ) )
            {
                cache.Add( p );
                yield return p;
            }
            // Then we use the cache to avoid recomputing the combinations.
            // Rationale: their should be much less SUT than regular assemblies, the first round will often not succeeds,
            // we'll often have to replay the list...
            // Note: the list's length depends on the number of parts (the depth of the testProjectFolder).
            foreach( var c in cache )
            {
                // Because of the .SUT suffix, we may have prefixes that are on the test project folder.
                var candidate = c.RemoveLastPart().AppendPart( targetName );
                if( !testProjectFolder.StartsWith( candidate ) )
                {
                    yield return c.RemoveLastPart().AppendPart( targetName );
                }
            }
        }
    }

    /// <summary>
    /// Enumerates a set of lookup paths from a folder starting with the best one (implements <see cref="IMonitorTestHelper.ClosestSUTProjectFolder"/>).
    /// </summary>
    /// <param name="rootCount">The root length: nothing will return above this one.</param>
    /// <param name="startFolder">The starting folder.</param>
    /// <param name="targetName">The leaf directory name to lookup.</param>
    /// <param name="skipDirectParentFolder">False to allow candidates to be parent folders of <paramref name="startFolder"/>.</param>
    /// <returns>The closest paths in order of preference.</returns>
    internal static IEnumerable<NormalizedPath> GetClosestCandidates( int rootCount,
                                                                      NormalizedPath startFolder,
                                                                      string targetName,
                                                                      bool skipDirectParentFolder = true )
    {
        var head = startFolder;
        var subPaths = new List<NormalizedPath>();

        static IEnumerable<NormalizedPath> WithSubPaths( NormalizedPath startFolder,
                                                         bool skipDirectParentFolder,
                                                         List<NormalizedPath> subPaths,
                                                         ref NormalizedPath head )
        {
            static IEnumerable<NormalizedPath> GenerateWithSubPaths( NormalizedPath startFolder,
                                                                     bool skipDirectParentFolder,
                                                                     List<NormalizedPath> subPaths,
                                                                     NormalizedPath head,
                                                                     string lastPart )
            {
                foreach( var subPath in subPaths )
                {
                    var p = head.Combine( subPath );
                    if( !skipDirectParentFolder || !startFolder.StartsWith( p ) )
                        yield return p;
                }
                int c = subPaths.Count;
                NormalizedPath h = new NormalizedPath( lastPart );
                for( int i = 0; i < c; i++ )
                {
                    subPaths.Add( h.Combine( subPaths[i] ) );
                }
            }

            var lastPart = head.LastPart;
            head = head.RemoveLastPart();
            return GenerateWithSubPaths( startFolder, skipDirectParentFolder, subPaths, head, lastPart );
        }

        subPaths.Add( targetName );
        yield return head.AppendPart( targetName );
        while( head.Parts.Count > rootCount )
        {
            foreach( var s in WithSubPaths( startFolder, skipDirectParentFolder, subPaths, ref head ) ) yield return s;
        }
    }

    /// <summary>
    /// Implements <see cref="IMonitorTestHelper.GetSetting(string)"/>.
    /// </summary>
    /// <param name="key">The setting key: parts separated by '/'.</param>
    /// <returns>The value or null if the environment variable is not defined or empty.</returns>
    internal static string? GetSetting( string key )
    {
        Throw.CheckNotNullOrEmptyArgument( key );
        var v = Environment.GetEnvironmentVariable( "TestHelper__" + key.Replace( "/", "__" ) );
        return string.IsNullOrEmpty( v ) ? null : v;
    }

    /// <summary>
    /// Triggers this type initializer and re-throws any initialization error
    /// that may have occurred.
    /// <para>
    /// This ensures that the static members and hooks are initialized. Accessing <see cref="MonitorTestHelper.TestHelper"/>
    /// calls this.
    /// </para>
    /// </summary>
    internal static void EnsureInitialized()
    {
        if( _initializationError != null ) _initializationError.Throw();
    }
}
