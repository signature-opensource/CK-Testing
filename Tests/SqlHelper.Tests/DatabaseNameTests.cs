using CK.Testing;
using NUnit.Framework;
using Shouldly;
using System;

namespace SqlHelperTests;

/// <summary>
/// Tests the computation of the database names. These tests do not use the server.
/// </summary>
[TestFixture]
public class DatabaseNameTests
{
    [TestCase( null, "" )]
    [TestCase( "", "" )]
    [TestCase( "wt", "_wt_wt" )]
    [TestCase( "feat-x1", "_wt_feat_x1" )]
    [TestCase( "Feat-X-é", "_wt_Feat_X__" )]
    [TestCase( "a.b c_D9", "_wt_a_b_c_D9" )]
    public void the_worktree_suffix_is_wt_and_the_identifier_with_only_ASCII_letters_digits_and_underscores( string? worktreeId, string expected )
    {
        SqlServerTestHelperExtensions.GetWorktreeDatabaseNameSuffix( worktreeId ).ShouldBe( expected );
    }

    [Test]
    public void the_worktree_marker_prevents_a_collision_with_another_project_of_the_main_checkout()
    {
        // With a plain '_' separator, "CK.DB.Auth.Tests" in the worktree "basic" gave "CKTEST_CK_DB_Auth_basic":
        // with a case insensitive collation, this is the database of "CK.DB.Auth.Basic.Tests" in the main checkout.
        var inWorktree = SqlServerTestHelperExtensions.GetDefaultDatabaseName( null, "CK.DB.Auth.Tests", SqlServerTestHelperExtensions.GetWorktreeDatabaseNameSuffix( "basic" ) );
        var inMain = SqlServerTestHelperExtensions.GetDefaultDatabaseName( null, "CK.DB.Auth.Basic.Tests", "" );
        inWorktree.ShouldBe( "CKTEST_CK_DB_Auth_wt_basic" );
        inMain.ShouldBe( "CKTEST_CK_DB_Auth_Basic" );
        String.Equals( inWorktree, inMain, StringComparison.OrdinalIgnoreCase ).ShouldBeFalse();
    }

    [Test]
    public void identifiers_that_differ_only_by_a_cleaned_character_give_the_same_suffix()
    {
        // Known limit: "feat-x" and "feat_x" are two valid worktree identifiers.
        SqlServerTestHelperExtensions.GetWorktreeDatabaseNameSuffix( "feat-x" )
            .ShouldBe( SqlServerTestHelperExtensions.GetWorktreeDatabaseNameSuffix( "feat_x" ) );
    }

    [TestCase( "SqlHelper.Tests", "", "CKTEST_SqlHelper" )]
    [TestCase( "SqlHelper.Tests", "_wt_feat_x1", "CKTEST_SqlHelper_wt_feat_x1" )]
    [TestCase( "My-Project.Tests", "_wt_a", "CKTEST_My_Project_wt_a" )]
    [TestCase( "OtherTests", "_wt_a", "CKTEST_Other_wt_a" )]
    public void the_default_name_derives_from_the_test_project_name_and_ends_with_the_suffix( string testProjectName, string suffix, string expected )
    {
        SqlServerTestHelperExtensions.GetDefaultDatabaseName( null, testProjectName, suffix ).ShouldBe( expected );
    }

    [TestCase( "", "MyDb" )]
    [TestCase( "_wt_a", "MyDb_wt_a" )]
    public void a_configured_name_also_ends_with_the_suffix( string suffix, string expected )
    {
        SqlServerTestHelperExtensions.GetDefaultDatabaseName( "MyDb", "SqlHelper.Tests", suffix ).ShouldBe( expected );
    }

    [Test]
    public void the_name_has_124_characters_or_less_and_the_suffix_is_never_lost()
    {
        // Measured on SQL Server 2022: "create database" accepts 124 characters, not 125.
        SqlServerTestHelperExtensions.MaxDatabaseNameLength.ShouldBe( 124 );
        SqlServerTestHelperExtensions.MinKeptSuffixLength.ShouldBe( 8 );

        // It fits.
        var name110 = new string( 'N', 110 );
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name110, "_wt_1234567890" ).ShouldBe( name110 + "_wt_1234567890" );

        // The end of the suffix is removed: more than 8 characters of the suffix remain.
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name110, "_wt_12345678901" ).ShouldBe( name110 + "_wt_1234567890" );

        // The room is too small for 8 characters of the suffix: the end of the name is removed too.
        var name120 = new string( 'N', 120 );
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name120, "_wt_12345678" ).ShouldBe( new string( 'N', 116 ) + "_wt_1234" );
        var name124 = new string( 'N', 124 );
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name124, "_wt_12345678" ).ShouldBe( new string( 'N', 116 ) + "_wt_1234" );
        var name200 = new string( 'N', 200 );
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name200, "_wt_12345678" ).ShouldBe( new string( 'N', 116 ) + "_wt_1234" );

        // A suffix shorter than 8 characters is kept in full.
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name124, "_wt_a" ).ShouldBe( new string( 'N', 119 ) + "_wt_a" );

        // With an empty suffix, a name that is too long is not changed: SQL Server rejects it.
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( name200, "" ).ShouldBe( name200 );
    }

    [Test]
    public void an_empty_suffix_changes_nothing()
    {
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( "TEST_SetupEngine_Version", "" ).ShouldBe( "TEST_SetupEngine_Version" );
        SqlServerTestHelperExtensions.ApplyDatabaseNameSuffix( "TEST_SetupEngine_Version", "_wt_a" ).ShouldBe( "TEST_SetupEngine_Version_wt_a" );
    }
}
