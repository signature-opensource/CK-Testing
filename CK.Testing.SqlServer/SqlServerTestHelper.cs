using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CK.Core;
using CK.Testing.SqlServer;

namespace CK.Testing
{

    /// <summary>
    /// Provides default implementation of <see cref="ISqlServerTestHelperCore"/>.
    /// </summary>
    public class SqlServerTestHelper : ISqlServerTestHelperCore
    {
        readonly TestHelperConfiguration _config;
        readonly IMonitorTestHelper _monitor;
        readonly string _confMasterConnectionString;
        readonly string _confDBName;
        readonly string _confCollation;
        readonly int _confCompatibilityLevel;

        // This is level obtained by opening a connection to the masterConnectionString.
        // DoGetDefaultDatabaseOptions updates it at its first call.
        int _maxCompatibilityLevel;
        static Version? _serverVersion;
        static ISqlServerDatabaseOptions? _defaultDatabaseOptions;
        static SqlConnectionStringBuilder? _connectionBuilder;

        event EventHandler<SqlServerDatabaseEventArgs>? _onEvent;
        BackupManager? _backup;

        internal SqlServerTestHelper( TestHelperConfiguration config, IMonitorTestHelper monitor )
        {
            _config = config;
            _monitor = monitor;

            var c = config.Declare( "SqlServer/DatabaseName", $"The default database name. When not configured this is built based on the project name '{_monitor.TestProjectName}'.", null );
            var dbName = c.ConfiguredValue;
            if( dbName == null )
            {
                var n = "CKTEST_" + _monitor.TestProjectName.Replace( '.', '_' ).Replace( '-', '_' );
                dbName = n.Replace( "_Tests", String.Empty );
                if( dbName == n ) dbName = n.Replace( "Tests", String.Empty );
            }
            c.SetDefaultValue( dbName );
            Debug.Assert( c.CurrentValue != null, "We set a non null default." );
            _confDBName = c.CurrentValue;

            _confMasterConnectionString = _config.Declare( "SqlServer/MasterConnectionString",
                                                           "Server=.;Database=master;Integrated Security=SSPI;TrustServerCertificate=True",
                                                           "The connection string to the master database of the Sql Server that will be used by the tests.",
                                                           null ).Value;

            _confCollation = config.Declare( "SqlServer/Collation",
                                             "Latin1_General_100_BIN2",
                                             "The expected collation of the Sql Server. The EnsureDatabase method drops and recreates a database if the collation differ.",
                                             null ).Value;

            _confCompatibilityLevel = config.DeclareInt32( "SqlServer/CompatibilityLevel",
                                                           "The compatibility level to use. The major of the Sql Server product version multiplied by 10 (it is 130 for Sql Server 2016 which product version is 13.0). Defaults to 0 that uses the current version of the server.",
                                                           () => _confCompatibilityLevel.ToString() ).Value ?? 0;

        }

        string ISqlServerTestHelperCore.MasterConnectionString => EnsureMasterConnection().ToString();

        ISqlServerDatabaseOptions ISqlServerTestHelperCore.DefaultDatabaseOptions => DoGetDefaultDatabaseOptions();

        SqlServerDatabaseOptions? ISqlServerTestHelperCore.GetDatabaseOptions( string databaseName ) => DoGetDatabaseOptions( databaseName );

        string ISqlServerTestHelperCore.GetConnectionString( string? databaseName ) => DoGetConnectionString( databaseName );

        bool ISqlServerTestHelperCore.ExecuteScripts( IEnumerable<string> scripts, string? databaseName ) => DoExecuteScripts( scripts, databaseName );

        bool ISqlServerTestHelperCore.ExecuteScripts( string scripts, string? databaseName ) => DoExecuteScripts( scripts, databaseName );

        bool ISqlServerTestHelperCore.EnsureDatabase( ISqlServerDatabaseOptions? o, bool reset ) => DoEnsureDatabase( o, reset );

        internal bool DoEnsureDatabase( ISqlServerDatabaseOptions? o, bool reset )
        {
            // Calls DoGetDefaultDatabaseOptions to update _maxCompatibilityLevel.
            var def = DoGetDefaultDatabaseOptions();
            if( o == null ) o = def;
            using( _monitor.Monitor.OpenInfo( $"Ensuring database '{o}'." ) )
            {
                try
                {
                    int normalizedLevel = o.CompatibilityLevel;
                    if( normalizedLevel == _maxCompatibilityLevel ) normalizedLevel = 0;
                    var current = DoGetDatabaseOptions( o.DatabaseName );
                    if( current != null )
                    {
                        if( !reset )
                        {
                            reset = current.Collation != o.Collation || current.CompatibilityLevel != normalizedLevel;
                        }
                        if( !reset )
                        {
                            _monitor.Monitor.CloseGroup( "Database already exists, collation and compatibility level match." );
                            return false;
                        }
                        Debug.Assert( current.DatabaseName != null );
                        _monitor.Monitor.Info( $"Current is {current}. Must be recreated." );
                        DoDrop( current.DatabaseName, true );
                    }
                    string create = $@"create database {o.DatabaseName} collate {o.Collation};";
                    if( normalizedLevel != 0 )
                    {
                        create += Environment.NewLine + "go" + Environment.NewLine;
                        create += $"alter database {o.DatabaseName} set compatibility_level = {normalizedLevel}";
                    }
                    using( var oCon = new SqlConnection( EnsureMasterConnection().ToString() ) )
                    using( var cmd = new SqlCommand( create, oCon ) )
                    {
                        oCon.Open();
                        cmd.ExecuteNonQuery();
                    }
                    var opt = DoGetDatabaseOptions( o.DatabaseName );
                    Debug.Assert( opt != null );
                    _onEvent?.Invoke( this, new SqlServerDatabaseEventArgs( opt, false ) );
                    return true;
                }
                catch( Exception ex )
                {
                    _monitor.Monitor.Error( ex );
                    throw;
                }
            }
        }

        SqlConnection ISqlServerTestHelperCore.CreateOpenedConnection( string? databaseName ) => DoCreateOpenedConnection( databaseName );

        Task<SqlConnection> ISqlServerTestHelperCore.CreateOpenedConnectionAsync( string? databaseName ) => DoCreateOpenedConnectionAsync( databaseName );

        void ISqlServerTestHelperCore.DropDatabase( string? databaseName, bool closeExistingConnections )
        {
            var o = databaseName == null ? DoGetDefaultDatabaseOptions() : DoGetDatabaseOptions( databaseName );
            if( o != null )
            {
                DoDrop( o.DatabaseName, closeExistingConnections );
                _onEvent?.Invoke( this, new SqlServerDatabaseEventArgs( o, true ) );
            }
        }

        event EventHandler<SqlServerDatabaseEventArgs>? ISqlServerTestHelperCore.OnDatabaseCreatedOrDropped
        {
            add => _onEvent += value;
            remove => _onEvent -= value;
        }

        SqlConnection DoCreateOpenedConnection( string? databaseName )
        {
            var oCon = new SqlConnection( DoGetConnectionString( databaseName ) );
            oCon.Open();
            return oCon;
        }

        async Task<SqlConnection> DoCreateOpenedConnectionAsync( string? databaseName )
        {
            var oCon = new SqlConnection( DoGetConnectionString( databaseName ) );
            await oCon.OpenAsync();
            return oCon;
        }

        SqlServerDatabaseOptions? DoGetDatabaseOptions( string? dbName )
        {
            if( dbName == null ) return new SqlServerDatabaseOptions( DoGetDefaultDatabaseOptions() );
            const string info = "select compatibility_level, IsNull( collation_name, convert(sysname,SERVERPROPERTY('Collation'))) from sys.databases where name=@N;";
            using( var oCon = new SqlConnection( EnsureMasterConnection().ToString() ) )
            using( var cmd = new SqlCommand( info, oCon ) )
            {
                oCon.Open();
                cmd.Parameters.AddWithValue( "@N", dbName );
                using( var r = cmd.ExecuteReader() )
                {
                    if( !r.Read() ) return null;
                    int level = r.GetByte( 0 );
                    if( level == _maxCompatibilityLevel ) level = 0;
                    return new SqlServerDatabaseOptions( dbName )
                    {
                        CompatibilityLevel = level,
                        Collation = r.GetString( 1 )
                    };
                }
            }
        }

        internal ISqlServerDatabaseOptions DoGetDefaultDatabaseOptions()
        {
            if( _defaultDatabaseOptions == null )
            {
                _monitor.OnlyOnce( () =>
                {
                    using( var oCon = new SqlConnection( EnsureMasterConnection().ToString() ) )
                    using( var cmd = new SqlCommand( "select SERVERPROPERTY('ProductVersion')", oCon ) )
                    {
                        oCon.Open();
                        _serverVersion = Version.Parse( (string)cmd.ExecuteScalar() );
                        _maxCompatibilityLevel = _serverVersion.Major * 10;
                    }
                    _defaultDatabaseOptions = new SqlServerDatabaseOptions( _confDBName )
                    {
                        Collation = _confCollation,
                        CompatibilityLevel = _confCompatibilityLevel
                    };
                } );
            }
            return _defaultDatabaseOptions!;
        }

        void DoDrop( string dbName, bool closeExistingConnections )
        {
            using( _monitor.Monitor.OpenInfo( $"Dropping database '{dbName}' ({(closeExistingConnections ? "" : "NOT ")}closing existing connections)." ) )
            {
                SqlConnection.ClearAllPools();
                try
                {
                    using( var oCon = new SqlConnection( EnsureMasterConnection().ToString() ) )
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
                            _monitor.Monitor.CloseGroup( "Database does not exist." );
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
                    _monitor.Monitor.Error( ex );
                    throw;
                }
            }
        }

        BackupManager ISqlServerTestHelperCore.Backup => _backup ??= new BackupManager( this, _monitor );

        #region Execute scripts
        static readonly Regex _rGo = new Regex( @"^\s*GO(?:\s|$)+", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled );

        internal bool DoExecuteScripts( string script, string? databaseName ) => DoExecuteScripts( new[] { script }, databaseName );

        internal bool DoExecuteScripts( IEnumerable<string> scripts, string? databaseName )
        {
            using( var oCon = DoCreateOpenedConnection( databaseName ) )
            using( _monitor.Monitor.OpenInfo( $"Executing scripts on '{oCon.Database}'." ) )
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
                                _monitor.Monitor.Debug( s );
                                cmd.CommandText = s;
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch( Exception ex )
                {
                    _monitor.Monitor.Error( ex );
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

        #endregion

        string DoGetConnectionString( string? dbName )
        {
            if( dbName == null ) dbName = DoGetDefaultDatabaseOptions().DatabaseName;
            var c = EnsureMasterConnection();
            string savedMaster = c.InitialCatalog;
            c.InitialCatalog = dbName;
            string result = c.ToString();
            c.InitialCatalog = savedMaster;
            return result;
        }

        SqlConnectionStringBuilder EnsureMasterConnection()
        {
            if( _connectionBuilder == null )
            {
                _monitor.OnlyOnce( () =>
                {
                    _connectionBuilder = new SqlConnectionStringBuilder( _confMasterConnectionString );
                } );
            }
            return _connectionBuilder!;
        }

        /// <summary>
        /// Gets the <see cref="ISqlServerTestHelper"/> default implementation.
        /// </summary>
        public static ISqlServerTestHelper TestHelper => TestHelperResolver.Default.Resolve<ISqlServerTestHelper>();


    }
}
