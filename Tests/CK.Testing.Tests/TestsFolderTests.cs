using CK.Core;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;

namespace CK.Testing.Tests;


[TestFixture]
public class TestsFolderTests
{
    NormalizedPath _root;

    [SetUp]
    public void CreateRoot()
    {
        _root = Path.Combine( Path.GetTempPath(), "CK-Testing-TestsFolder-" + Guid.NewGuid().ToString( "N" ) );
        Directory.CreateDirectory( _root );
    }

    [TearDown]
    public void DeleteRoot()
    {
        Directory.Delete( _root, recursive: true );
    }

    [Test]
    public void the_Tests_folder_in_the_solution_is_found()
    {
        var solution = _root.AppendPart( "Repo" );
        Directory.CreateDirectory( solution.AppendPart( ".git" ) );
        var testProject = solution.AppendPart( "Tests" ).AppendPart( "P.Tests" );
        Directory.CreateDirectory( testProject );

        StaticBasicTestHelper.FindTestsFolder( testProject, solution ).ShouldBe( solution.AppendPart( "Tests" ) );
    }

    [Test]
    public void the_deepest_Tests_folder_in_the_solution_wins()
    {
        var solution = _root.AppendPart( "Repo" );
        Directory.CreateDirectory( solution.AppendPart( ".git" ) );
        var testProject = solution.AppendPart( "Tests" ).AppendPart( "Group" ).AppendPart( "Tests" ).AppendPart( "P.Tests" );
        Directory.CreateDirectory( testProject );

        StaticBasicTestHelper.FindTestsFolder( testProject, solution )
            .ShouldBe( solution.AppendPart( "Tests" ).AppendPart( "Group" ).AppendPart( "Tests" ) );
    }

    [Test]
    public void the_test_project_folder_itself_and_the_solution_folder_are_not_Tests_folders()
    {
        // No enclosing repository above "Tests" (the solution folder).
        var solution = _root.AppendPart( "Tests" );
        Directory.CreateDirectory( solution.AppendPart( ".git" ) );
        var testProject = solution.AppendPart( "Tests" );
        Directory.CreateDirectory( testProject );

        StaticBasicTestHelper.FindTestsFolder( testProject, solution ).ShouldBeNull();
    }

    [Test]
    public void a_submodule_in_the_Tests_folder_of_its_superproject_uses_this_Tests_folder()
    {
        // Super/.git (directory), Super/Tests/Sub/.git (file of a submodule), Super/Tests/Sub/Sub.Tests (no "Tests" folder in Sub).
        var super = _root.AppendPart( "Super" );
        Directory.CreateDirectory( super.AppendPart( ".git" ) );
        var sub = super.AppendPart( "Tests" ).AppendPart( "Sub" );
        Directory.CreateDirectory( sub );
        File.WriteAllText( sub.AppendPart( ".git" ), "gitdir: ../../.git/modules/Sub\n" );
        var testProject = sub.AppendPart( "Sub.Tests" );
        Directory.CreateDirectory( testProject );

        StaticBasicTestHelper.FindTestsFolder( testProject, sub ).ShouldBe( super.AppendPart( "Tests" ) );
    }

    [Test]
    public void a_Tests_folder_above_the_solution_is_ignored_without_an_enclosing_repository()
    {
        // A linked worktree in "<root>/Tests/wt" is not in a repository: "<root>/Tests" does not count.
        var worktree = _root.AppendPart( "Tests" ).AppendPart( "wt" );
        Directory.CreateDirectory( worktree );
        File.WriteAllText( worktree.AppendPart( ".git" ), "gitdir: X:/Main/.git/worktrees/wt\n" );
        var testProject = worktree.AppendPart( "P.Tests" );
        Directory.CreateDirectory( testProject );

        StaticBasicTestHelper.FindTestsFolder( testProject, worktree ).ShouldBeNull();
    }

    [Test]
    public void a_Tests_folder_above_the_enclosing_repository_is_ignored()
    {
        // Tests/Super/.git, Tests/Super/Libs/Sub/Sub.Tests: "Tests" is above the enclosing repository.
        var super = _root.AppendPart( "Tests" ).AppendPart( "Super" );
        Directory.CreateDirectory( super.AppendPart( ".git" ) );
        var sub = super.AppendPart( "Libs" ).AppendPart( "Sub" );
        Directory.CreateDirectory( sub );
        File.WriteAllText( sub.AppendPart( ".git" ), "gitdir: ../../.git/modules/Sub\n" );
        var testProject = sub.AppendPart( "Sub.Tests" );
        Directory.CreateDirectory( testProject );

        StaticBasicTestHelper.FindTestsFolder( testProject, sub ).ShouldBeNull();
    }
}
