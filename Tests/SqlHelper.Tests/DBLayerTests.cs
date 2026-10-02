using CK.Core;
using CK.Testing;
using Shouldly;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace SqlHelperTests;

[TestFixture]
public class DBLayerTests
{
    static int _consoleToggleCount = 0;

    [Explicit]
    [Test]
    public void toggle_console_output()
    {
        TestHelper.Monitor.Info( $"Before Toggle n°{++_consoleToggleCount}" );
        TestHelper.LogToConsole = !TestHelper.LogToConsole;
        TestHelper.Monitor.Info( $"After Toggle n°{_consoleToggleCount}" );
    }

    [Explicit]
    [TestCase( "closeExistingConnections" )]
    [TestCase( "" )]
    public void drop_database( string mode )
    {
        TestHelper.DropDatabase( closeExistingConnections: mode == "closeExistingConnections" );
    }

    [TestCase( "reset" )]
    [TestCase( "" )]
    [Explicit]
    public void ensure_database( string reset )
    {
        TestHelper.EnsureDatabase( reset: reset == "reset" );
    }


    [Test]
    public void dropping_database_multiple_times()
    {
        TestHelper.EnsureDatabase( reset: false );
        TestHelper.DropDatabase();
        TestHelper.DropDatabase();
    }

    [Test]
    public void Execute_create_script_on_Database_and_Drop()
    {
        TestHelper.EnsureDatabase( reset: true );
        TestHelper.ExecuteScripts( File.ReadAllText( TestHelper.TestProjectFolder.AppendPart( "Model.Sql" ) ) );
        TestHelper.DropDatabase();
    }

    [Test]
    public void connection_string()
    {
        TestHelper.Monitor.Info( $"Current User: {Environment.UserDomainName}/{Environment.UserName}" );
        var c = TestHelper.MasterConnectionString;
        c.ShouldContain( "master" );
        c.ShouldContain( "Integrated Security" );
        var c2 = TestHelper.GetConnectionString( "Toto" );
        c2.ShouldContain( "Toto" );
        c.ShouldContain( "Integrated Security" );
        c = TestHelper.MasterConnectionString;
        c.ShouldContain( "master" );
        c.ShouldContain( "Integrated Security" );
    }

    [Test]
    public void OnDatabaseCreatedOrDropped_static_event_reaches_a_mixin_helper()
    {
        // Resolving the IStupidTestHelper creates the StupidTestHelper that subscribes to the static event.
        var stupid = StupidTestHelper.TestHelper;
        int before = stupid.CountOfStupidMethodCalls;
        var dbName = TestHelper.DefaultDatabaseOptions.DatabaseName;

        TestHelper.EnsureDatabase( reset: true ).ShouldBeTrue();
        stupid.LastDatabaseCreatedOrDroppedName.ShouldBe( dbName );
        stupid.CountOfStupidMethodCalls.ShouldBe( before + 1 );

        // The IStupidTestHelper is an IMonitorTestHelper: it exposes the extension members too.
        stupid.DropDatabase();
        stupid.CountOfStupidMethodCalls.ShouldBe( before + 2 );
    }

    [Test]
    public void default_database_name_derives_from_the_test_project_name()
    {
        // No configuration sets "SqlServer/DatabaseName" in this repository.
        var options = TestHelper.DefaultDatabaseOptions;
        options.DatabaseName.ShouldBe( "CKTEST_SqlHelper" );
        // The configuration is read once: the default options are always the same object.
        TestHelper.DefaultDatabaseOptions.ShouldBeSameAs( options );
        TestHelper.GetConnectionString().ShouldBe( TestHelper.GetConnectionString( "CKTEST_SqlHelper" ) );
    }

    /// <summary>
    /// Calls <see cref="CK.Testing.SqlServer.BackupManager.CreateBackup(string?)"/> on the
    /// default database (<see cref="SqlServerTestHelperExtensions.extension(IMonitorTestHelper).DefaultDatabaseOptions"/>).
    /// </summary>
    [Test]
    [Explicit]
    public void backup_create()
    {
        Assert.That( TestHelper.Backup.CreateBackup() != null, "Backup should be possible." );
    }

    /// <summary>
    /// Calls <see cref="CK.Testing.SqlServer.BackupManager.RestoreBackup(string?, int)"/> on the
    /// default database (<see cref="SqlServerTestHelperExtensions.extension(IMonitorTestHelper).DefaultDatabaseOptions"/>).
    /// </summary>
    [TestCase( "0 - Most recent one." )]
    [TestCase( "1" )]
    [TestCase( "2" )]
    [TestCase( "3" )]
    [TestCase( "4" )]
    [TestCase( "5" )]
    [TestCase( "X - Oldest one." )]
    [Explicit]
    public void backup_restore( string what )
    {
        if( !int.TryParse( what, out var index ) )
        {
            index = what[0] == 'X' ? Int32.MaxValue : 0;
        }
        Assert.That( TestHelper.Backup.RestoreBackup( null, index ) != null, "Restoring should be possible." );
    }

    /// <summary>
    /// Dumps all the available backup files in <see cref="CK.Testing.SqlServer.BackupManager.BackupFolder"/>
    /// as information into the <see cref="CK.Testing.Monitoring.IMonitorTestHelperCore.Monitor"/>.
    /// </summary>
    [Test]
    [Explicit]
    public void backup_list()
    {
        var all = TestHelper.Backup.GetAllBackups();
        using( TestHelper.Monitor.OpenInfo( $"There is {all.Count} backups available in '{TestHelper.Backup.BackupFolder}'." ) )
        {
            TestHelper.Monitor.Info( all.Select( a => $"n° {a.Index} - {a.FileName}" ).Concatenate( Environment.NewLine ) );
        }
    }
}
