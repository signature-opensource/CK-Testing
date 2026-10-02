using CK.Core;
using CK.Testing.SqlServer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Testing;

/// <summary>
/// Extends <see cref="IMonitorTestHelper"/> with SQL Server helpers: create, drop, back up and restore
/// a test database, and execute scripts.
/// <para>
/// Use <c>using static CK.Testing.MonitorTestHelper;</c> to get the <c>TestHelper</c>.
/// The <c>CK.Testing</c> namespace must also be in scope (<c>using CK.Testing;</c>) to see these members.
/// </para>
/// <para>
/// These operations are dangerous. No database name is protected: a drop is sent for any name,
/// including a system database. The default database name starts with "CKTEST_" and this prefix
/// is the only protection for a real database. A configured "SqlServer/DatabaseName" is used as-is.
/// </para>
/// <para>
/// The configuration is read once per process, from <see cref="TestHelperConfiguration.Default"/>,
/// when a member is used for the first time.
/// </para>
/// </summary>
public static class SqlServerTestHelperExtensions
{
    static readonly object _lock = new object();
    static readonly ConditionalWeakTable<IMonitorTestHelper, BackupManager> _backups = new ConditionalWeakTable<IMonitorTestHelper, BackupManager>();
    static readonly Regex _rGo = new Regex( @"^\s*GO(?:\s|$)+", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled );

    // Set once by GetConfiguration.
    static Configuration? _configuration;
    // Set once by GetMaxCompatibilityLevel. 0 until the server answers.
    static int _maxCompatibilityLevel;

    /// <summary>
    /// Fires when a database is created, reset or dropped by <see cref="EnsureDatabase(IMonitorTestHelper, ISqlServerDatabaseOptions?, bool)"/>
    /// or <see cref="DropDatabase(IMonitorTestHelper, string?, bool)"/>.
    /// The sender is the <see cref="IMonitorTestHelper"/> that executes the operation.
    /// <para>
    /// This is a static event: extension events do not exist.
    /// </para>
    /// </summary>
    public static event EventHandler<SqlServerDatabaseEventArgs>? OnDatabaseCreatedOrDropped;

    /// <param name="helper">This helper.</param>
    extension( IMonitorTestHelper helper )
    {
        /// <summary>
        /// Gets the connection string to the master database. The "SqlServer/MasterConnectionString" configuration
        /// gives it. The default is "Server=.;Database=master;Integrated Security=SSPI;TrustServerCertificate=True".
        /// <para>
        /// The value is the normalized output of a <see cref="SqlConnectionStringBuilder"/>, not the literal configured string.
        /// </para>
        /// <para>
        /// If no configuration file sets it, the environment variable "TestHelper__SqlServer__MasterConnectionString"
        /// (or "TestHelper__MasterConnectionString") can set it.
        /// </para>
        /// </summary>
        public string MasterConnectionString => GetConfiguration( helper ).MasterConnectionString;

        /// <summary>
        /// Gets the options of the default test database.
        /// <para>
        /// The name is the "SqlServer/DatabaseName" configuration. When it is not configured, the name is "CKTEST_"
        /// followed by the <see cref="IBasicTestHelper.TestProjectName"/>: the '.' and '-' become '_' and the "Tests"
        /// part is removed. For example, the project "SqlHelper.Tests" gives "CKTEST_SqlHelper".
        /// </para>
        /// <para>
        /// The collation is the "SqlServer/Collation" configuration. It defaults to "Latin1_General_100_BIN2".
        /// </para>
        /// <para>
        /// The compatibility level is the "SqlServer/CompatibilityLevel" configuration. It defaults to 0: a database
        /// is created with the current level of the server.
        /// </para>
        /// </summary>
        public ISqlServerDatabaseOptions DefaultDatabaseOptions => GetConfiguration( helper ).DefaultDatabaseOptions;

        /// <summary>
        /// Gets the connection string to a database, based on <see cref="extension(IMonitorTestHelper).MasterConnectionString"/>.
        /// </summary>
        /// <param name="databaseName">The database name. Defaults to the <see cref="extension(IMonitorTestHelper).DefaultDatabaseOptions"/> name.</param>
        /// <returns>The connection string to the database.</returns>
        public string GetConnectionString( string? databaseName = null ) => DoGetConnectionString( helper, databaseName );

        /// <summary>
        /// Gets the options of an existing database. This opens a connection to the server.
        /// <para>
        /// When <paramref name="databaseName"/> is null, this returns a copy of the <see cref="extension(IMonitorTestHelper).DefaultDatabaseOptions"/>
        /// and does not check that the database exists.
        /// </para>
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>The options, or null if the database does not exist.</returns>
        public SqlServerDatabaseOptions? GetDatabaseOptions( string databaseName ) => DoGetDatabaseOptions( helper, databaseName );

        /// <summary>
        /// Makes sure that a database exists with the expected collation and compatibility level.
        /// If the database exists with other options, or if <paramref name="reset"/> is true, this drops and creates it again.
        /// <para>
        /// This fires <see cref="OnDatabaseCreatedOrDropped"/> when it creates the database.
        /// </para>
        /// </summary>
        /// <param name="o">The database options. Defaults to <see cref="extension(IMonitorTestHelper).DefaultDatabaseOptions"/>.</param>
        /// <param name="reset">True to drop and create the database again.</param>
        /// <returns>True if the database is created or reset. False if it exists with the same options.</returns>
        public bool EnsureDatabase( ISqlServerDatabaseOptions? o = null, bool reset = false ) => DoEnsureDatabase( helper, o, reset );

        /// <summary>
        /// Drops a database. Nothing happens if the database does not exist.
        /// <para>
        /// This fires <see cref="OnDatabaseCreatedOrDropped"/> when the database exists.
        /// </para>
        /// </summary>
        /// <param name="databaseName">The database name to drop. Defaults to the <see cref="extension(IMonitorTestHelper).DefaultDatabaseOptions"/> name.</param>
        /// <param name="closeExistingConnections">By default, the existing connections are closed by force.</param>
        public void DropDatabase( string? databaseName = null, bool closeExistingConnections = true ) => DoDropDatabase( helper, databaseName, closeExistingConnections );

        /// <summary>
        /// Creates an opened connection to a database. The caller must dispose it.
        /// </summary>
        /// <param name="databaseName">The database name. Defaults to the <see cref="extension(IMonitorTestHelper).DefaultDatabaseOptions"/> name.</param>
        /// <returns>An opened connection.</returns>
        public SqlConnection CreateOpenedConnection( string? databaseName = null ) => DoCreateOpenedConnection( helper, databaseName );

        /// <summary>
        /// Creates an opened connection to a database. The caller must dispose it.
        /// </summary>
        /// <param name="databaseName">The database name. Defaults to the <see cref="extension(IMonitorTestHelper).DefaultDatabaseOptions"/> name.</param>
        /// <returns>An opened connection.</returns>
        public Task<SqlConnection> CreateOpenedConnectionAsync( string? databaseName = null ) => DoCreateOpenedConnectionAsync( helper, databaseName );

        /// <summary>
        /// Executes scripts. A script can contain 'GO' separators. A 'GO' can be lowercase, but it must be alone on its line.
        /// </summary>
        /// <param name="scripts">The scripts to execute.</param>
        /// <param name="databaseName">The database name. Defaults to the <see cref="extension(IMonitorTestHelper).DefaultDatabaseOptions"/> name.</param>
        /// <returns>True on success, false if an error occurred. The error is logged.</returns>
        public bool ExecuteScripts( IEnumerable<string> scripts, string? databaseName = null ) => DoExecuteScripts( helper, scripts, databaseName );

        /// <summary>
        /// Executes a script. The script can contain 'GO' separators. A 'GO' can be lowercase, but it must be alone on its line.
        /// </summary>
        /// <param name="scripts">The script to execute.</param>
        /// <param name="databaseName">The database name. Defaults to the <see cref="extension(IMonitorTestHelper).DefaultDatabaseOptions"/> name.</param>
        /// <returns>True on success, false if an error occurred. The error is logged.</returns>
        public bool ExecuteScripts( string scripts, string? databaseName = null ) => DoExecuteScripts( helper, new[] { scripts }, databaseName );

        /// <summary>
        /// Gets a helper that backs up and restores databases.
        /// </summary>
        public BackupManager Backup => _backups.GetValue( helper, static h => new BackupManager( h ) );
    }

    sealed class Configuration
    {
        public Configuration( string masterConnectionString, SqlServerDatabaseOptions defaultDatabaseOptions )
        {
            MasterConnectionString = masterConnectionString;
            DefaultDatabaseOptions = defaultDatabaseOptions;
        }

        public string MasterConnectionString { get; }

        public SqlServerDatabaseOptions DefaultDatabaseOptions { get; }
    }

    static Configuration GetConfiguration( IBasicTestHelper helper )
    {
        var c = Volatile.Read( ref _configuration );
        if( c == null )
        {
            lock( _lock )
            {
                c = _configuration;
                if( c == null )
                {
                    c = ReadConfiguration( TestHelperConfiguration.Default, helper.TestProjectName );
                    Volatile.Write( ref _configuration, c );
                }
            }
        }
        return c;
    }

    // This is the only place that reads the configuration.
    // A TestHelperConfiguration throws when a key is declared twice: GetConfiguration calls this once.
    static Configuration ReadConfiguration( TestHelperConfiguration config, string testProjectName )
    {
        var cName = config.Declare( "SqlServer/DatabaseName",
                                    $"The default database name. When not configured this is built based on the project name '{testProjectName}'.",
                                    null );
        var dbName = GetDefaultDatabaseName( cName.ConfiguredValue, testProjectName );
        cName.SetDefaultValue( dbName );

        var masterConnectionString = config.Declare( "SqlServer/MasterConnectionString",
                                                     "Server=.;Database=master;Integrated Security=SSPI;TrustServerCertificate=True",
                                                     "The connection string to the master database of the Sql Server that will be used by the tests.",
                                                     null ).Value;

        var collation = config.Declare( "SqlServer/Collation",
                                        "Latin1_General_100_BIN2",
                                        "The expected collation of the Sql Server. The EnsureDatabase method drops and recreates a database if the collation differ.",
                                        null ).Value;

        int compatibilityLevel = 0;
        compatibilityLevel = config.DeclareInt32( "SqlServer/CompatibilityLevel",
                                                  "The compatibility level to use. The major of the Sql Server product version multiplied by 10 (it is 130 for Sql Server 2016 which product version is 13.0). Defaults to 0 that uses the current version of the server.",
                                                  () => compatibilityLevel.ToString() ).Value ?? 0;

        var master = new SqlConnectionStringBuilder( masterConnectionString ).ToString();
        var defaultOptions = new SqlServerDatabaseOptions( dbName )
        {
            Collation = collation,
            CompatibilityLevel = compatibilityLevel
        };
        return new Configuration( master, defaultOptions );
    }

    // This is the only place that computes the default database name.
    // A configured name is used as-is. Else the name derives from the test project name.
    static string GetDefaultDatabaseName( string? configuredName, string testProjectName )
    {
        if( configuredName != null ) return configuredName;
        var n = "CKTEST_" + testProjectName.Replace( '.', '_' ).Replace( '-', '_' );
        var dbName = n.Replace( "_Tests", String.Empty );
        if( dbName == n ) dbName = n.Replace( "Tests", String.Empty );
        return dbName;
    }

    // Reads the server version once. A level equal to this maximum is normalized to 0.
    // If the server does not answer, this throws and the next call tries again.
    static int GetMaxCompatibilityLevel( IMonitorTestHelper helper )
    {
        int level = Volatile.Read( ref _maxCompatibilityLevel );
        if( level == 0 )
        {
            lock( _lock )
            {
                level = _maxCompatibilityLevel;
                if( level == 0 )
                {
                    using( var oCon = new SqlConnection( GetConfiguration( helper ).MasterConnectionString ) )
                    using( var cmd = new SqlCommand( "select SERVERPROPERTY('ProductVersion')", oCon ) )
                    {
                        oCon.Open();
                        var serverVersion = Version.Parse( (string)cmd.ExecuteScalar() );
                        level = serverVersion.Major * 10;
                    }
                    Volatile.Write( ref _maxCompatibilityLevel, level );
                }
            }
        }
        return level;
    }

    static string DoGetConnectionString( IMonitorTestHelper helper, string? databaseName )
    {
        var c = GetConfiguration( helper );
        var b = new SqlConnectionStringBuilder( c.MasterConnectionString )
        {
            InitialCatalog = databaseName ?? c.DefaultDatabaseOptions.DatabaseName
        };
        return b.ToString();
    }

    static SqlServerDatabaseOptions? DoGetDatabaseOptions( IMonitorTestHelper helper, string? databaseName )
    {
        if( databaseName == null ) return new SqlServerDatabaseOptions( GetConfiguration( helper ).DefaultDatabaseOptions );
        int maxLevel = GetMaxCompatibilityLevel( helper );
        const string info = "select compatibility_level, IsNull( collation_name, convert(sysname,SERVERPROPERTY('Collation'))) from sys.databases where name=@N;";
        using( var oCon = new SqlConnection( GetConfiguration( helper ).MasterConnectionString ) )
        using( var cmd = new SqlCommand( info, oCon ) )
        {
            oCon.Open();
            cmd.Parameters.AddWithValue( "@N", databaseName );
            using( var r = cmd.ExecuteReader() )
            {
                if( !r.Read() ) return null;
                int level = r.GetByte( 0 );
                if( level == maxLevel ) level = 0;
                return new SqlServerDatabaseOptions( databaseName )
                {
                    CompatibilityLevel = level,
                    Collation = r.GetString( 1 )
                };
            }
        }
    }

    static bool DoEnsureDatabase( IMonitorTestHelper helper, ISqlServerDatabaseOptions? o, bool reset )
    {
        o ??= GetConfiguration( helper ).DefaultDatabaseOptions;
        using( helper.Monitor.OpenInfo( $"Ensuring database '{o}'." ) )
        {
            try
            {
                int normalizedLevel = o.CompatibilityLevel;
                if( normalizedLevel == GetMaxCompatibilityLevel( helper ) ) normalizedLevel = 0;
                var current = DoGetDatabaseOptions( helper, o.DatabaseName );
                if( current != null )
                {
                    if( !reset )
                    {
                        reset = current.Collation != o.Collation || current.CompatibilityLevel != normalizedLevel;
                    }
                    if( !reset )
                    {
                        helper.Monitor.CloseGroup( "Database already exists, collation and compatibility level match." );
                        return false;
                    }
                    Debug.Assert( current.DatabaseName != null );
                    helper.Monitor.Info( $"Current is {current}. Must be recreated." );
                    DoDrop( helper, current.DatabaseName, true );
                }
                string create = $@"create database {o.DatabaseName} collate {o.Collation};";
                if( normalizedLevel != 0 )
                {
                    create += Environment.NewLine + "go" + Environment.NewLine;
                    create += $"alter database {o.DatabaseName} set compatibility_level = {normalizedLevel}";
                }
                using( var oCon = new SqlConnection( GetConfiguration( helper ).MasterConnectionString ) )
                using( var cmd = new SqlCommand( create, oCon ) )
                {
                    oCon.Open();
                    cmd.ExecuteNonQuery();
                }
                var opt = DoGetDatabaseOptions( helper, o.DatabaseName );
                Debug.Assert( opt != null );
                OnDatabaseCreatedOrDropped?.Invoke( helper, new SqlServerDatabaseEventArgs( opt, false ) );
                return true;
            }
            catch( Exception ex )
            {
                helper.Monitor.Error( ex );
                throw;
            }
        }
    }

    static void DoDropDatabase( IMonitorTestHelper helper, string? databaseName, bool closeExistingConnections )
    {
        ISqlServerDatabaseOptions? o = databaseName == null
                                        ? GetConfiguration( helper ).DefaultDatabaseOptions
                                        : DoGetDatabaseOptions( helper, databaseName );
        if( o != null )
        {
            DoDrop( helper, o.DatabaseName, closeExistingConnections );
            OnDatabaseCreatedOrDropped?.Invoke( helper, new SqlServerDatabaseEventArgs( o, true ) );
        }
    }

    static void DoDrop( IMonitorTestHelper helper, string dbName, bool closeExistingConnections )
    {
        using( helper.Monitor.OpenInfo( $"Dropping database '{dbName}' ({(closeExistingConnections ? "" : "NOT ")}closing existing connections)." ) )
        {
            SqlConnection.ClearAllPools();
            try
            {
                using( var oCon = new SqlConnection( GetConfiguration( helper ).MasterConnectionString ) )
                using( var cmd = new SqlCommand() )
                {
                    cmd.Connection = oCon;
                    oCon.Open();

                    var exec = $"if db_id('{dbName}') is not null begin ";
                    if( closeExistingConnections )
                    {
                        exec += $"alter database [{dbName}] set single_user with rollback immediate;";
                    }
                    exec += $"drop database [{dbName}]; select 1; end else begin select 0; end";

                    cmd.CommandText = exec;
                    if( (int)cmd.ExecuteScalar() == 0 )
                    {
                        helper.Monitor.CloseGroup( "Database does not exist." );
                    }
                    else
                    {
                        cmd.CommandText = $"exec msdb.dbo.sp_delete_database_backuphistory @database_name = N'{dbName}';";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch( Exception ex )
            {
                helper.Monitor.Error( ex );
                throw;
            }
        }
    }

    static SqlConnection DoCreateOpenedConnection( IMonitorTestHelper helper, string? databaseName )
    {
        var oCon = new SqlConnection( DoGetConnectionString( helper, databaseName ) );
        oCon.Open();
        return oCon;
    }

    static async Task<SqlConnection> DoCreateOpenedConnectionAsync( IMonitorTestHelper helper, string? databaseName )
    {
        var oCon = new SqlConnection( DoGetConnectionString( helper, databaseName ) );
        await oCon.OpenAsync();
        return oCon;
    }

    static bool DoExecuteScripts( IMonitorTestHelper helper, IEnumerable<string> scripts, string? databaseName )
    {
        using( var oCon = DoCreateOpenedConnection( helper, databaseName ) )
        using( helper.Monitor.OpenInfo( $"Executing scripts on '{oCon.Database}'." ) )
        using( var cmd = new SqlCommand() )
        {
            try
            {
                cmd.Connection = oCon;
                foreach( var g in scripts )
                {
                    if( !String.IsNullOrWhiteSpace( g ) )
                    {
                        foreach( var s in SplitGoSeparator( g ) )
                        {
                            helper.Monitor.Debug( s );
                            cmd.CommandText = s;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                helper.Monitor.Error( ex );
                return false;
            }
        }
        return true;
    }

    static IEnumerable<string> SplitGoSeparator( string script )
    {
        if( !string.IsNullOrWhiteSpace( script ) )
        {
            int curBeg = 0;
            for( Match goDelim = _rGo.Match( script ); goDelim.Success; goDelim = goDelim.NextMatch() )
            {
                int lenScript = goDelim.Index - curBeg;
                if( lenScript > 0 )
                {
                    yield return script.Substring( curBeg, lenScript );
                }
                curBeg = goDelim.Index + goDelim.Length;
            }
            if( script.Length > curBeg )
            {
                yield return script.Substring( curBeg ).TrimEnd();
            }
        }
    }
}
