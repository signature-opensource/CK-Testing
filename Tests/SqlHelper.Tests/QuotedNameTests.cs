using CK.Testing;
using CK.Testing.SqlServer;
using NUnit.Framework;
using Shouldly;
using System.IO;
using static CK.Testing.MonitorTestHelper;

namespace SqlHelperTests;

[TestFixture]
public class QuotedNameTests
{
    [Test]
    public void QuoteName_and_QuoteString_double_the_closing_characters()
    {
        SqlServerTestHelperExtensions.QuoteName( "A-B c" ).ShouldBe( "[A-B c]" );
        SqlServerTestHelperExtensions.QuoteName( "A]B" ).ShouldBe( "[A]]B]" );
        SqlServerTestHelperExtensions.QuoteString( "it's" ).ShouldBe( "N'it''s'" );
    }

    [Test]
    public void a_database_name_with_special_characters_can_be_created_used_and_dropped()
    {
        // '-', ' ', ']' and ''' all require quoting.
        var name = TestHelper.GetScopedDatabaseName( "CKTEST_SqlHelper Quoted-Name]'" );
        try
        {
            TestHelper.EnsureDatabase( new SqlServerDatabaseOptions( name ), reset: true ).ShouldBeTrue();
            TestHelper.EnsureDatabase( new SqlServerDatabaseOptions( name ) ).ShouldBeFalse( "The database exists with the same options." );
            TestHelper.ExecuteScripts( "create table dbo.T( Id int not null ); insert into dbo.T( Id ) values( 3712 );", name ).ShouldBeTrue();
            CountRows( name ).ShouldBe( 1 );
            TestHelper.DropDatabase( name );
            TestHelper.GetDatabaseOptions( name ).ShouldBeNull();
        }
        finally
        {
            TestHelper.DropDatabase( name );
        }
        TestHelper.GetDatabaseOptions( name ).ShouldBeNull();
    }

    [Test]
    [Explicit( "The SQL Server service account needs write access to the DBBackup folder of this test project. A checkout under a user profile (for example in %TEMP%) usually does not give it." )]
    public void a_database_name_with_special_characters_can_be_backed_up_and_restored()
    {
        // '-', ' ', ']' and ''' all require quoting.
        var name = TestHelper.GetScopedDatabaseName( "CKTEST_SqlHelper Backup-Name]'" );
        var defaultName = TestHelper.DefaultDatabaseOptions.DatabaseName;
        try
        {
            TestHelper.EnsureDatabase( new SqlServerDatabaseOptions( name ), reset: true ).ShouldBeTrue();
            TestHelper.ExecuteScripts( "create table dbo.T( Id int not null ); insert into dbo.T( Id ) values( 3712 );", name ).ShouldBeTrue();

            var backup = TestHelper.Backup.CreateBackup( name );
            backup.ShouldNotBeNull();
            backup.Value.DatabaseName.ShouldBe( name );

            TestHelper.ExecuteScripts( "delete from dbo.T;", name ).ShouldBeTrue();
            CountRows( name ).ShouldBe( 0 );
            TestHelper.Backup.RestoreBackup( name ).ShouldNotBeNull();
            CountRows( name ).ShouldBe( 1 );

            // The restore creates a database that does not exist, and it does not create the default database.
            TestHelper.DropDatabase( name );
            TestHelper.DropDatabase();
            TestHelper.GetDatabaseOptions( name ).ShouldBeNull();
            TestHelper.Backup.RestoreBackup( name ).ShouldNotBeNull();
            CountRows( name ).ShouldBe( 1 );
            TestHelper.GetDatabaseOptions( defaultName ).ShouldBeNull( "RestoreBackup must not create the default database." );
        }
        finally
        {
            TestHelper.DropDatabase( name );
            foreach( var b in TestHelper.Backup.GetBackups( name ) )
            {
                File.Delete( TestHelper.Backup.BackupFolder.AppendPart( b.FileName ) );
            }
        }
        TestHelper.GetDatabaseOptions( name ).ShouldBeNull();
        TestHelper.Backup.GetBackups( name ).ShouldBeEmpty();
    }

    [Test]
    public void the_longest_name_can_be_created_and_dropped()
    {
        var name = SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( "CKTEST_SqlHelper_Long" + new string( 'L', 200 ), "_wt_12345678" );
        name.Length.ShouldBe( SqlServerTestHelperExtensions.MaxDatabaseNameLength );
        try
        {
            TestHelper.EnsureDatabase( new SqlServerDatabaseOptions( name ), reset: true ).ShouldBeTrue();
            TestHelper.GetDatabaseOptions( name ).ShouldNotBeNull();
        }
        finally
        {
            TestHelper.DropDatabase( name );
        }
        TestHelper.GetDatabaseOptions( name ).ShouldBeNull();
    }

    static int CountRows( string databaseName )
    {
        using( var c = TestHelper.CreateOpenedConnection( databaseName ) )
        using( var cmd = c.CreateCommand() )
        {
            cmd.CommandText = "select count(*) from dbo.T;";
            return (int)cmd.ExecuteScalar()!;
        }
    }
}
