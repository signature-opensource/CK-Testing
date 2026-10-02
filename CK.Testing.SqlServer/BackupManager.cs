using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CK.Testing.SqlServer
{
    /// <summary>
    /// Supports the backup and restore operations. Use the <see cref="SqlServerTestHelperExtensions"/> <c>Backup</c>
    /// property to get it.
    /// </summary>
    public class BackupManager
    {
        internal BackupManager( IMonitorTestHelper helper )
        {
            Helper = helper;
        }

        IMonitorTestHelper Helper { get; }

        /// <summary>
        /// Captures simple ordered backup file.
        /// </summary>
        public readonly struct Backup
        {
            /// <summary>
            /// The name of the database.
            /// </summary>
            public readonly string DatabaseName;

            /// <summary>
            /// The current backup number (0 is the most recent one) for the <see cref="DatabaseName"/>.
            /// </summary>
            public readonly int Index;

            /// <summary>
            /// The Utc date and time of the backup.
            /// </summary>
            public readonly DateTime Date;

            /// <summary>
            /// Gets the backup file name.
            /// </summary>
            public string FileName => GetFileName( DatabaseName, Date );

            /// <summary>
            /// Initializes a new Backup instance.
            /// </summary>
            /// <param name="dbName">The database name.</param>
            /// <param name="date">Backup date (must be Utc).</param>
            /// <param name="index">Index of the backup.</param>
            public Backup( string dbName, DateTime date, int index )
            {
                Throw.CheckNotNullOrEmptyArgument( dbName );
                Throw.CheckArgument( date.Kind == DateTimeKind.Utc );
                Throw.CheckArgument( index >= 0 );
                DatabaseName = dbName;
                Index = index;
                Date = date;
            }

        }

        /// <summary>
        /// Gets the backup folder that is <see cref="IBasicTestHelper.TestProjectFolder"/>/DBBackup.
        /// </summary>
        public NormalizedPath BackupFolder => Helper.TestProjectFolder.AppendPart( "DBBackup" );

        static string GetFileName( string dbName, DateTime date ) => $"{dbName} {date:yy-MM-dd HH-mm-ss}.bak";

        /// <summary>
        /// Gets all the available backups found in <see cref="BackupFolder"/>, regardless of their <see cref="Backup.DatabaseName"/>.
        /// </summary>
        /// <returns>The list of available backups.</returns>
        public IReadOnlyList<Backup> GetAllBackups()
        {
            return Directory.Exists( BackupFolder )
                    ? Directory.GetFiles( BackupFolder )
                               .Select( f => MatchFileName( f ) )
                               .Where( m => m.Success )
                               .Select( m => (m.Groups[1].Value,
                                              new DateTime( int.Parse( m.Groups[2].Value ), int.Parse( m.Groups[3].Value ), int.Parse( m.Groups[4].Value ),
                                                            int.Parse( m.Groups[5].Value ), int.Parse( m.Groups[6].Value ), int.Parse( m.Groups[7].Value ),
                                                            DateTimeKind.Utc )) )
                               .GroupBy( m => m.Value )
                               .SelectMany( g => g.OrderByDescending( p => p.Item2 ).Select( ( p, idx ) => new Backup( p.Value, p.Item2, idx ) ) )
                               .ToArray()
                    : Array.Empty<Backup>();
        }

        /// <summary>
        /// Gets the existing backups for a database in <see cref="BackupFolder"/>.
        /// </summary>
        /// <param name="dbName">Database name. Defaults to the name of the default database options.</param>
        /// <returns>The list of backups. Can be empty.</returns>
        public IReadOnlyList<Backup> GetBackups( string? dbName = null )
        {
            dbName ??= Helper.DefaultDatabaseOptions.DatabaseName;
            return Directory.Exists( BackupFolder )
                    ? Directory.GetFiles( BackupFolder, dbName + " *.bak" )
                               .Select( f => MatchFileName( f ) )
                               .Where( m => m.Success && m.Groups[1].Value == dbName )
                               .Select( m => new DateTime( int.Parse( m.Groups[2].Value ), int.Parse( m.Groups[3].Value ), int.Parse( m.Groups[4].Value ),
                                                           int.Parse( m.Groups[5].Value ), int.Parse( m.Groups[6].Value ), int.Parse( m.Groups[7].Value ),
                                                           DateTimeKind.Utc ) )
                               .OrderByDescending( d => d )
                               .Select( ( p, idx ) => new Backup( dbName, p, idx ) )
                               .ToArray()
                    : Array.Empty<Backup>();
        }

        static Match MatchFileName( string f )
        {
            return Regex.Match( f, @"(?<1>[^/\\]*) (?<2>\d\d)-(?<3>\d\d)-(?<4>\d\d) (?<5>\d\d)-(?<6>\d\d)-(?<7>\d\d)\.bak$", RegexOptions.CultureInvariant );
        }

        /// <summary>
        /// Creates a new backup in the <see cref="BackupFolder"/>.
        /// </summary>
        /// <param name="dbName">Database name to backup. Defaults to the name of the default database options.</param>
        /// <returns>The new backup description or null on error.</returns>
        public Backup? CreateBackup( string? dbName = null )
        {
            dbName ??= Helper.DefaultDatabaseOptions.DatabaseName;

            var t = DateTime.UtcNow;
            t = new DateTime( t.Ticks - (t.Ticks % TimeSpan.TicksPerSecond), t.Kind );

            Directory.CreateDirectory( BackupFolder );
            var fName = BackupFolder.AppendPart( GetFileName( dbName, t ) );
            using( Helper.Monitor.OpenInfo( $"Creating a Backup for '{dbName}'." ) )
            {
                if( Helper.ExecuteScripts( $"backup database [{dbName}] to disk = N'{fName}' with name = N'{dbName}', copy_only, noformat, init, skip, compression;", dbName ) )
                {
                    if( File.Exists( fName ) )
                    {
                        var r = new Backup( dbName, t, 0 );
                        Helper.Monitor.CloseGroup( r.FileName );
                        return r;
                    }
                    else
                    {
                        Helper.Monitor.Error( $"Unable to find file '{fName}' that should have been created." );
                    }
                }
                Helper.Monitor.CloseGroup( "Failed." );
                return null;
            }
        }

        /// <summary>
        /// Restores a backup. See <see cref="GetBackups(string?)"/>.
        /// </summary>
        /// <param name="dbName">Database name to restore. Defaults to the name of the default database options.</param>
        /// <param name="index">
        /// By default, the most recent backup is restored.
        /// You can use <see cref="int.MaxValue"/> to restore the oldest available backup.
        /// </param>
        /// <returns>The restored backup or null if no backup exists.</returns>
        public Backup? RestoreBackup( string? dbName = null, int index = 0 )
        {
            Throw.CheckOutOfRangeArgument( index >= 0 );
            dbName ??= Helper.DefaultDatabaseOptions.DatabaseName;
            var all = GetBackups( dbName );
            if( all.Count == 0 )
            {
                Helper.Monitor.Warn( $"No backup found for database '{dbName}'." );
                return null;
            }
            Backup backup;
            string msg;
            if( all.Count == 1 )
            {
                msg = $"There is only one backup available for database '{dbName}'. Restoring it.";
                backup = all[0];
            }
            else if( index >= all.Count - 1 )
            {
                msg = $"Restoring the oldest available backup for database '{dbName}'.";
                backup = all[all.Count - 1];
            }
            else if( index == 0 )
            {
                msg = $"Restoring the most recent available backup for database '{dbName}'.";
                backup = all[0];
            }
            else
            {
                msg = $"Restoring available backup n°{index} for database '{dbName}'.";
                backup = all[index];
            }
            using( Helper.Monitor.OpenInfo( msg ) )
            {
                Helper.EnsureDatabase( null, false );
                var script = $@"use [master]; alter database [{dbName}] set single_user with rollback immediate;
restore database [{dbName}] from disk = N'{BackupFolder.AppendPart( backup.FileName )}' with file = 1,  nounload, replace;
alter database [{dbName}] set multi_user;";

                if( Helper.ExecuteScripts( script, dbName ) )
                {
                    Helper.Monitor.CloseGroup( "Success." );
                    return backup;
                }
                Helper.Monitor.CloseGroup( "Failed." );
                return null;
            }
        }
    }
}
