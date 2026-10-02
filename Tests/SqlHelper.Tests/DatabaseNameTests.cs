using CK.Testing;
using NUnit.Framework;
using Shouldly;

namespace SqlHelperTests;

/// <summary>
/// Tests the computation of the database names. These tests do not use the server.
/// </summary>
[TestFixture]
public class DatabaseNameTests
{
    [TestCase( null, "" )]
    [TestCase( "", "" )]
    [TestCase( "wt", "_wt" )]
    [TestCase( "feat-x1", "_feat_x1" )]
    [TestCase( "Feat-X-é", "_Feat_X__" )]
    [TestCase( "a.b c_D9", "_a_b_c_D9" )]
    public void the_worktree_suffix_keeps_only_ASCII_letters_digits_and_underscores( string? worktreeId, string expected )
    {
        SqlServerTestHelperExtensions.GetWorktreeDatabaseNameSuffix( worktreeId ).ShouldBe( expected );
    }

    [TestCase( "SqlHelper.Tests", "", "CKTEST_SqlHelper" )]
    [TestCase( "SqlHelper.Tests", "_feat_x1", "CKTEST_SqlHelper_feat_x1" )]
    [TestCase( "My-Project.Tests", "_wt", "CKTEST_My_Project_wt" )]
    [TestCase( "OtherTests", "_wt", "CKTEST_Other_wt" )]
    public void the_default_name_derives_from_the_test_project_name_and_ends_with_the_suffix( string testProjectName, string suffix, string expected )
    {
        SqlServerTestHelperExtensions.GetDefaultDatabaseName( null, testProjectName, suffix ).ShouldBe( expected );
    }

    [TestCase( "", "MyDb" )]
    [TestCase( "_wt", "MyDb_wt" )]
    public void a_configured_name_also_ends_with_the_suffix( string suffix, string expected )
    {
        SqlServerTestHelperExtensions.GetDefaultDatabaseName( "MyDb", "SqlHelper.Tests", suffix ).ShouldBe( expected );
    }

    [Test]
    public void the_name_has_128_characters_or_less_and_only_the_suffix_is_truncated()
    {
        SqlServerTestHelperExtensions.MaxDatabaseNameLength.ShouldBe( 128 );

        var name120 = new string( 'N', 120 );
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name120, "_1234567" ).ShouldBe( name120 + "_1234567" );
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name120, "_12345678" ).ShouldBe( name120 + "_1234567" );
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name120, "_1234567890" ).Length.ShouldBe( 128 );

        var name127 = new string( 'N', 127 );
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name127, "_wt" ).ShouldBe( name127 + "_" );

        var name128 = new string( 'N', 128 );
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name128, "_wt" ).ShouldBe( name128 );

        var name130 = new string( 'N', 130 );
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name130, "_wt" ).ShouldBe( name130, "A name that is too long is not changed: SQL Server rejects it." );
    }

    [Test]
    public void an_empty_suffix_changes_nothing()
    {
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( "TEST_SetupEngine_Version", "" ).ShouldBe( "TEST_SetupEngine_Version" );
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( "TEST_SetupEngine_Version", "_wt" ).ShouldBe( "TEST_SetupEngine_Version_wt" );
    }
}
