using CK.Testing;
using CK.Testing.SqlServer;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace SqlHelperTests;

[TestFixture]
public class CompatibilityLevelTests
{
    [Test]
    public void EnsureDatabase_creates_a_database_with_a_compatibility_level_lower_than_the_server_level()
    {
        // 130 is SQL Server 2016. The server must be more recent: a level equal to the server level is normalized to 0.
        var name = TestHelper.GetScopedDatabaseName( "CKTEST_SqlHelper_Level" );
        var options = new SqlServerDatabaseOptions( name ) { CompatibilityLevel = 130 };
        try
        {
            TestHelper.EnsureDatabase( options, reset: true ).ShouldBeTrue();
            var current = TestHelper.GetDatabaseOptions( name );
            current.ShouldNotBeNull();
            current.CompatibilityLevel.ShouldBe( 130 );
            TestHelper.EnsureDatabase( options ).ShouldBeFalse( "The database exists with the same options." );
        }
        finally
        {
            TestHelper.DropDatabase( name );
        }
        TestHelper.GetDatabaseOptions( name ).ShouldBeNull();
    }
}
