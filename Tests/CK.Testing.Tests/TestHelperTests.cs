using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using static CK.Testing.MonitorTestHelper;

namespace CK.Testing.Tests;

[TestFixture]
public class TestHelperTests
{
    [Test]
    public void TestHelper_is_a_singleton()
    {
        TestHelper.ShouldBeSameAs( MonitorTestHelper.TestHelper );
    }

    [Test]
    public void paths_of_this_test_project()
    {
        // In a git worktree, the SolutionFolder can have any name, but the SolutionName is the name of the main repository.
        TestHelper.SolutionName.ShouldBe( "CK-Testing" );
        TestHelper.TestProjectName.ShouldBe( "CK.Testing.Tests" );
        TestHelper.SolutionFolder.ShouldBe( TestHelper.TestProjectFolder.RemoveLastPart().RemoveLastPart(), "This test project is <SolutionFolder>/Tests/CK.Testing.Tests." );
        File.Exists( TestHelper.SolutionFolder.AppendPart( "CK-Testing.slnx" ) ).ShouldBeTrue();
        var dotGit = TestHelper.SolutionFolder.AppendPart( ".git" );
        (Directory.Exists( dotGit ) || File.Exists( dotGit )).ShouldBeTrue( "A main checkout has a .git directory, a git worktree has a .git file." );
        TestHelper.ClosestSUTProjectFolder.ShouldBe( TestHelper.SolutionFolder.AppendPart( "CK.Testing" ) );
        TestHelper.LogFolder.ShouldBe( TestHelper.TestProjectFolder.AppendPart( "Logs" ) );
        TestHelper.BinFolder.ShouldBe( TestHelper.TestProjectFolder.Combine( TestHelper.PathToBin ) );
        TestHelper.PathToBin.Parts[0].ShouldBe( "bin" );
        TestHelper.PathToBin.Parts[1].ShouldBe( TestHelper.BuildConfiguration );
    }

    [Test]
    public void settings_are_environment_variables()
    {
        const string variable = "TestHelper__Test__Setting";
        try
        {
            TestHelper.GetSetting( "Test/Setting" ).ShouldBeNull();
            Environment.SetEnvironmentVariable( variable, "A value" );
            TestHelper.GetSetting( "Test/Setting" ).ShouldBe( "A value" );
        }
        finally
        {
            Environment.SetEnvironmentVariable( variable, null );
        }
        TestHelper.GetSetting( "Test/Setting" ).ShouldBeNull();
    }
}
