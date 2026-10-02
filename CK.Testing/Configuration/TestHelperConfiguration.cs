using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CK.Testing;


/// <summary>
/// Simple configuration that reads its content from all "*.TestHelper.config" (in lexicographical 
/// order) and then "TestHelper.config" files in folders from <see cref="IBasicTestHelper.SolutionFolder"/> 
/// down to the current execution path.
/// Once all these files are applied, environment variables that start with "TestHelper::" prefix are applied.
/// </summary>
public sealed partial class TestHelperConfiguration
{
    readonly Dictionary<NormalizedPath, Value> _config;
    readonly List<IValue> _unconfiguredValues;
    internal IBasicTestHelper? _basic;


    /// <summary>
    /// Initializes a new <see cref="TestHelperConfiguration"/>.
    /// </summary>
    public TestHelperConfiguration()
    {
        StaticBasicTestHelper.EnsureInitialized();
        _config = new Dictionary<NormalizedPath, Value>();
        _unconfiguredValues = new List<IValue>();
        ApplyFilesConfig( BasicTestHelper._binFolder );
        SimpleReadFromEnvironment();
    }

    /// <summary>
    /// Declares a configuration key.
    /// <para>
    /// This must be called once and only once per key otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="key">The path of the key to find.</param>
    /// <param name="description">Required non empty description.</param>
    /// <param name="editableValue">Optional getter for the current value if it is editable.</param>
    /// <param name="previousNames">Optional deprecated aliases.</param>
    /// <returns>The configuration (may be configured or not).</returns>
    public IValue Declare( NormalizedPath key, string description, Func<string>? editableValue, params string[] previousNames )
    {
        Throw.CheckArgument( !key.IsEmptyPath );
        Throw.CheckNotNullOrEmptyArgument( description );
        IValue? result = FindConfigured( key, description, editableValue, key );
        if( result != null ) return result;
        if( previousNames != null )
        {
            foreach( var old in previousNames )
            {
                result = FindConfigured( key, description, editableValue, old );
                if( result != null ) return result;
            }
        }
        result = new UnconfiguredValue( key, description, editableValue );
        int idxUnconf = _unconfiguredValues.IndexOf( u => u.Key == key );
        if( idxUnconf >= 0 )
        {
            Value.ThrowAlreadyReadConfiguration( key );
        }
        _unconfiguredValues.Add( result );
        return result;

        IValue? FindConfigured( NormalizedPath key, string description, Func<string>? editableValue, NormalizedPath lookup )
        {
            while( lookup.HasParts )
            {
                if( _config.TryGetValue( lookup, out var result ) )
                {
                    result.SetUsageInfoOnlyOnce( key, description, editableValue, lookup );
                    return result;
                }
                if( lookup.Parts.Count == 1 ) break;
                lookup = lookup.RemovePart( lookup.Parts.Count - 2 );
            }
            return null;
        }
    }

    /// <summary>
    /// Declares a configuration key with a non null <paramref name="defaultValue"/>.
    /// This calls <see cref="IValue.SetDefaultValue(string?)"/> and returns the configured or default value.
    /// <para>
    /// This must be called once and only once per key otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="key">The path of the key to find.</param>
    /// <param name="defaultValue">Default value to use if the key is not configured.</param>
    /// <param name="description">Required non empty description.</param>
    /// <param name="editableValue">Optional getter for the current value if it is editable.</param>
    /// <param name="previousNames">Optional deprecated aliases.</param>
    /// <returns>The configuration and its configured or default value.</returns>
    public (IValue Config, string Value) Declare( NormalizedPath key,
                                                  string defaultValue,
                                                  string description,
                                                  Func<string>? editableValue,
                                                  params string[] previousNames )
    {
        Throw.CheckNotNullArgument( defaultValue );
        var config = Declare( key, description, editableValue, previousNames );
        if( !config.IsEditable ) config.SetDefaultValue( defaultValue );
        return (config, config.ConfiguredValue ?? defaultValue);
    }

    /// <summary>
    /// Declares a configuration key, considering the configuration value as an absolute path
    /// or relative to its <see cref="IValue.FileBasePath"/>.
    /// <para>
    /// Placeholders {BuildConfiguration}, {TestProjectName}, {PathToBin} and {SolutionName} can appear anywhere in the Value
    /// and are replaced with their respective values.
    /// </para>
    /// <para>
    /// The Value can start with {BinFolder}, {SolutionFolder}, {TestProjectFolder} or {ClosestSUTProjectFolder}.
    /// If the Value does not start with one of this 4 paths and the Path is not <see cref="NormalizedPath.IsRooted"/>, the
    /// path is relative to its <see cref="IValue.FileBasePath"/>.
    /// </para>
    /// <para>
    /// If there is no '{' (ie. there is no unresolved placeholder), all '/../' are automatically resolved.
    /// If a '{'  remains in the path, the dots are not resolved: this is up to the code that will use the path to resolve the placeholders
    /// and the dots (see <see cref="NormalizedPath.ResolveDots(int, bool)"/>).
    /// </para>
    /// <para>
    /// Note that if the value is not a valid path, result is what it is, without any warranty.
    /// </para>
    /// <para>
    /// This must be called once and only once per key otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="key">The path of the key to find.</param>
    /// <param name="description">Required non empty description.</param>
    /// <param name="editableValue">Optional getter for the current value if it is editable.</param>
    /// <param name="previousNames">Optional deprecated aliases.</param>
    /// <returns>The configuration and its value or null if not found.</returns>
    public (IValue Config, NormalizedPath? Value) DeclarePath( NormalizedPath key,
                                                               string description,
                                                               Func<string>? editableValue,
                                                               params string[] previousNames )
    {
        var config = Declare( key, description, editableValue, previousNames );
        return (config, config.ConfiguredValue != null ? new NormalizedPath( GetValueAsPath( config, config.ConfiguredValue ) ) : (NormalizedPath?)null);
    }

    string GetValueAsPath( IValue config, string path )
    {
        Value v = (Value)config;
        Debug.Assert( path != null && !v.FileBasePath.IsEmptyPath );

        string s = path.Trim()
                       .Replace( "{BuildConfiguration}", BasicTestHelper._buildConfiguration )
                       .Replace( "{TestProjectName}", BasicTestHelper._testProjectFolder.LastPart )
                       .Replace( "{PathToBin}", BasicTestHelper._pathToBin )
                       .Replace( "{SolutionName}", BasicTestHelper._solutionName );

        string SubPathNoRoot( string theV, int prefixLen )
        {
            if( theV.Length > prefixLen
                && (theV[prefixLen] == System.IO.Path.DirectorySeparatorChar
                     || theV[prefixLen] == System.IO.Path.AltDirectorySeparatorChar) )
            {
                ++prefixLen;
            }
            return s.Substring( prefixLen );
        }

        Debug.Assert( "{BinFolder}".Length == 11 );
        Debug.Assert( "{SolutionFolder}".Length == 16 );
        Debug.Assert( "{TestProjectFolder}".Length == 19 );
        Debug.Assert( "{ClosestSUTProjectFolder}".Length == 25 );
        NormalizedPath raw;
        if( s.StartsWith( "{BinFolder}", StringComparison.OrdinalIgnoreCase ) ) raw = BasicTestHelper._binFolder.Combine( SubPathNoRoot( s, 11 ) );
        else if( s.StartsWith( "{SolutionFolder}", StringComparison.OrdinalIgnoreCase ) ) raw = BasicTestHelper._solutionFolder.Combine( SubPathNoRoot( s, 16 ) );
        else if( s.StartsWith( "{TestProjectFolder}", StringComparison.OrdinalIgnoreCase ) ) raw = BasicTestHelper._testProjectFolder.Combine( SubPathNoRoot( s, 19 ) );
        else if( _basic != null && s.StartsWith( "{ClosestSUTProjectFolder}", StringComparison.OrdinalIgnoreCase ) ) raw = _basic.ClosestSUTProjectFolder.Combine( SubPathNoRoot( s, 26 ) );
        else
        {
            if( Path.IsPathRooted( s ) ) return Path.GetFullPath( s );
            if( s.Contains( '{' ) ) return s;
            raw = v.FileBasePath.Combine( s );
        }
        return raw.Path.IndexOf( '{' ) < 0 ? raw.ResolveDots() : raw;
    }

    /// <summary>
    /// Declares a configuration key with a boolean value.
    /// <para>
    /// This must be called once and only once per key otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="key">The path of the key to find.</param>
    /// <param name="description">Required non empty description.</param>
    /// <param name="editableValue">Optional getter for the current value if it is editable.</param>
    /// <param name="previousNames">Optional deprecated aliases.</param>
    /// <returns>The configuration and its value or null if not found.</returns>
    public (IValue Config, bool? Value) DeclareBoolean( NormalizedPath key,
                                                        string description,
                                                        Func<string>? editableValue,
                                                        params string[] previousNames )
    {
        var config = Declare( key, description, editableValue, previousNames );
        return (config, config.ConfiguredValue != null ? StringComparer.OrdinalIgnoreCase.Equals( config.ConfiguredValue, "true" ) : null);
    }

    /// <summary>
    /// Declares a configuration key with a boolean value and a <paramref name="defaultValue"/>.
    /// <para>
    /// This must be called once and only once per key otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="key">The path of the key to find.</param>
    /// <param name="defaultValue">Default value to use when not configured.</param>
    /// <param name="description">Required non empty description.</param>
    /// <param name="editableValue">Optional getter for the current value if it is editable.</param>
    /// <param name="previousNames">Optional deprecated aliases.</param>
    /// <returns>The configuration and its configured or default value.</returns>
    public (IValue Config, bool Value) DeclareBoolean( NormalizedPath key,
                                                       bool defaultValue,
                                                       string description,
                                                       Func<string>? editableValue,
                                                       params string[] previousNames )
    {
        var config = Declare( key, description, editableValue, previousNames );
        if( !config.IsEditable ) config.SetDefaultValue( defaultValue.ToString() );
        return (config, config.ConfiguredValue != null ? StringComparer.OrdinalIgnoreCase.Equals( config.ConfiguredValue, "true" ) : defaultValue);
    }

    /// <summary>
    /// Declares a configuration key with a boolean value.
    /// <para>
    /// This must be called once and only once per key otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="key">The path of the key to find.</param>
    /// <param name="description">Required non empty description.</param>
    /// <param name="editableValue">Optional getter for the current value if it is editable.</param>
    /// <param name="previousNames">Optional deprecated aliases.</param>
    /// <returns>The configuration and its value or null if not found.</returns>
    public (IValue Config, int? Value) DeclareInt32( NormalizedPath key, string description, Func<string>? editableValue, params string[] previousNames )
    {
        var config = Declare( key, description, editableValue, previousNames );
        return (config, config.ConfiguredValue != null ? Int32.Parse( config.ConfiguredValue ) : null);
    }


    /// <summary>
    /// Declares a configuration key with a int value and a <paramref name="defaultValue"/>.
    /// <para>
    /// This must be called once and only once per key otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="key">The path of the key to find.</param>
    /// <param name="defaultValue">Default value to use when not configured.</param>
    /// <param name="description">Required non empty description.</param>
    /// <param name="editableValue">Optional getter for the current value if it is editable.</param>
    /// <param name="previousNames">Optional deprecated aliases.</param>
    /// <returns>The configuration and its configured or default value.</returns>
    public (IValue Config, int Value) DeclareInt32( NormalizedPath key,
                                                    int defaultValue,
                                                    string description,
                                                    Func<string>? editableValue,
                                                    params string[] previousNames )
    {
        var config = Declare( key, description, editableValue, previousNames );
        if( !config.IsEditable ) config.SetDefaultValue( defaultValue.ToString() );
        return (config, config.ConfiguredValue != null ? Int32.Parse( config.ConfiguredValue ) : defaultValue);
    }

    /// <summary>
    /// Declares a configuration key with a <paramref name="separator"/> separated string
    /// as a set of strings. Strings are trimmed and empty strings are removed.
    /// <para>
    /// This must be called once and only once per key otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="key">The path of the key to find.</param>
    /// <param name="description">Required non empty description.</param>
    /// <param name="editableValue">Optional getter for the current value if it is editable.</param>
    /// <param name="separator">String separator.</param>
    /// <param name="previousNames">Optional deprecated aliases.</param>
    /// <returns>The values as paths.</returns>
    public (IValue Config, IEnumerable<string> Value) DeclareMultiStrings( NormalizedPath key,
                                                                           string description,
                                                                           Func<string>? editableValue,
                                                                           char separator = ';',
                                                                           params string[] previousNames )
    {
        var config = Declare( key, description, editableValue, previousNames );
        if( string.IsNullOrWhiteSpace( config.ConfiguredValue ) ) return (config, Array.Empty<string>());
        // Don't use |StringSplitOptions.RemoveEmptyEntries so that the normalized Value displays the duplicates ;; if any.
        var strings = config.ConfiguredValue.Split( separator, StringSplitOptions.TrimEntries );
        // Normalize the Value.
        var normalized = string.Join( separator, strings );
        config.SetNormalizedConfiguredValue( normalized );
        if( editableValue == null ) config.SetDefaultValue( normalized );
        return (config, strings.Where( x => x.Length > 0 ));
    }

    /// <summary>
    /// Declares a configuration key with a value that is semi colon ';' separated set of paths
    /// (see <see cref="DeclarePath(NormalizedPath, string, Func{string}?, string[])"/>).
    /// <para>
    /// This must be called once and only once per key otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="key">The path of the key to find.</param>
    /// <param name="description">Required non empty description.</param>
    /// <param name="editableValue">Optional getter for the current value if it is editable.</param>
    /// <param name="previousNames">Optional deprecated aliases.</param>
    /// <returns>The values as paths.</returns>
    public (IValue Config, IEnumerable<NormalizedPath> Value) DeclareMultiPaths( NormalizedPath key,
                                                                                 string description,
                                                                                 Func<string>? editableValue,
                                                                                 params string[] previousNames )
    {
        var (config, values) = DeclareMultiStrings( key, description, editableValue, ';', previousNames );
        // To be "perfect" we reset the default value set by MultiStrings (if editableValue is null) to
        // the normalized paths (but let the ConfigurationValue to what it is).

        // And since we need to enumerate, we concretize the enumerable.
        var result = values.Select( x => new NormalizedPath( GetValueAsPath( config, x ) ) );

        if( editableValue == null )
        {
            result = result.ToArray();
            var renormalized = string.Join( ';', result );
            config.SetDefaultValue( renormalized );
        }
        return (config, result);
    }

    /// <summary>
    /// Gets all the declared configuration keys (that may or not have a <see cref="IValue.ConfiguredValue"/>).
    /// </summary>
    public IEnumerable<IValue> DeclaredValues => _config.Values.Where( v => v.IsUsed ).Concat( _unconfiguredValues );

    /// <summary>
    /// Gets the useless configuration values (they can be removed from the configuration).
    /// </summary>
    public IEnumerable<IUnusedValue> UselessValues => _config.Values.Where( v => !v.IsUsed );

    void SetEntry( string key, NormalizedPath basePath, string value )
    {
        Debug.Assert( value != null );
        NormalizedPath k = NormalizeKey( key );
        if( basePath.IsEmptyPath ) basePath = BasicTestHelper._testProjectFolder;
        _config[k] = new Value( basePath, k, value );
    }

    static NormalizedPath NormalizeKey( string key )
    {
        return key.Replace( "::", NormalizedPath.DirectorySeparatorString )
                  .Replace( "__", NormalizedPath.DirectorySeparatorString );
    }

    void ApplyFilesConfig( NormalizedPath folder )
    {
        if( folder.Parts.Count > BasicTestHelper._solutionFolder.Parts.Count )
        {
            ApplyFilesConfig( folder.RemoveLastPart() );
        }
        foreach( var f in Directory.EnumerateFiles( folder, "*.TestHelper.config" ).OrderBy( n => n ) )
        {
            SimpleReadFromAppSetting( f );
        }
        var file = folder.AppendPart( "TestHelper.config" );
        if( File.Exists( file ) )
        {
            SimpleReadFromAppSetting( file );
        }
    }

    void SimpleReadFromAppSetting( NormalizedPath appConfigFile )
    {
        var basePath = appConfigFile.RemoveLastPart();
        XDocument doc = XDocument.Load( appConfigFile );
        Debug.Assert( doc.Root != null );
        foreach( var e in doc.Root.Elements( "appSettings" ).Elements() )
        {
            if( e.Name.LocalName == "add" )
            {
                SetEntry( e.AttributeRequired( "key" ).Value, basePath, e.AttributeRequired( "value" ).Value );
            }
            else if( e.Name.LocalName == "remove" )
            {
                var k = NormalizeKey( e.AttributeRequired( "key" ).Value );
                while( k.HasParts )
                {
                    _config.Remove( k );
                    k = k.RemoveFirstPart();
                }
            }
            else if( e.Name.LocalName == "clear" )
            {
                _config.Clear();
            }
            else throw new Exception( $"Only add, remove and clear child elements of appSettings are supported in {appConfigFile}." );
        }
    }

    void SimpleReadFromEnvironment()
    {
        const string prefix1 = "TestHelper::";
        const string prefix2 = "TestHelper__";
        Debug.Assert( prefix1.Length == 12 && prefix2.Length == 12 );
        var env = Environment.GetEnvironmentVariables()
                    .Cast<DictionaryEntry>()
                    .Select( e => new KeyValuePair<string, string>( (string)e.Key, (string)e.Value! ) )
                    .Where( t => t.Key.StartsWith( prefix1, StringComparison.OrdinalIgnoreCase )
                                 || t.Key.StartsWith( prefix2, StringComparison.OrdinalIgnoreCase ) )
                    .Select( t => new KeyValuePair<string, string>( t.Key.Substring( 12 ), t.Value ) );

        foreach( var kv in env ) SetEntry( kv.Key, BasicTestHelper._testProjectFolder, kv.Value );
    }

    /// <summary>
    /// Gets a singleton that exposes the global configuration.
    /// </summary>
    public static TestHelperConfiguration Default { get; } = new TestHelperConfiguration();
}
