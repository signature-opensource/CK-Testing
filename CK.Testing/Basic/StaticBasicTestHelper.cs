using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using CK.Core;

namespace CK.Testing;


/// <summary>
/// Static part of the implementation of <see cref="BasicTestHelper"/>.
/// </summary>
public partial class StaticBasicTestHelper
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
    static StaticBasicTestHelper()
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
    /// Triggers this type initializer and re-throws any initialization error
    /// that may have occurred.
    /// <para>
    /// This ensures that the basic static members and hooks are initialized.
    /// This is almost always useless to call this explicitly since as soon as any TestHelper
    /// object is implied, this core type initializer is called.
    /// </para>
    /// </summary>
    public static void EnsureInitialized()
    {
        if( _initializationError != null ) _initializationError.Throw();
    }
}
