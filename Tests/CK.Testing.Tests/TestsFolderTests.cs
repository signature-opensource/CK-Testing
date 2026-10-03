using CK.Core;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;

namespace CK.Testing.Tests;


/// <summary>
/// Tests the "Tests" folder rule of the initialization. Each test creates its git layout (main checkouts,
/// submodules, worktrees) in a temporary folder. The search never goes above a main checkout or a worktree,
/// so a git repository above the temporary folder has no effect.
/// </summary>
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
        var solution = CreateMainCheckout( _root.AppendPart( "Repo" ) );
        var testProject = CreateFolder( solution.AppendPart( "Tests" ).AppendPart( "P.Tests" ) );

        StaticTestHelper.FindTestsFolder( testProject, solution ).ShouldBe( solution.AppendPart( "Tests" ) );
    }

    [Test]
    public void the_deepest_Tests_folder_in_the_solution_wins()
    {
        var solution = CreateMainCheckout( _root.AppendPart( "Repo" ) );
        var testProject = CreateFolder( solution.AppendPart( "Tests" ).AppendPart( "Group" ).AppendPart( "Tests" ).AppendPart( "P.Tests" ) );

        StaticTestHelper.FindTestsFolder( testProject, solution )
            .ShouldBe( solution.AppendPart( "Tests" ).AppendPart( "Group" ).AppendPart( "Tests" ) );
    }

    [Test]
    public void the_test_project_folder_and_the_solution_folder_are_not_Tests_folders()
    {
        var solution = CreateMainCheckout( _root.AppendPart( "Tests" ) );
        var testProject = CreateFolder( solution.AppendPart( "Tests" ) );

        StaticTestHelper.FindTestsFolder( testProject, solution ).ShouldBeNull();
    }

    [Test]
    public void no_folder_above_a_main_checkout_is_tried()
    {
        // Outer is a main checkout, and Outer/Tests/Repo is another main checkout (not a submodule).
        var outer = CreateMainCheckout( _root.AppendPart( "Outer" ) );
        var solution = CreateMainCheckout( outer.AppendPart( "Tests" ).AppendPart( "Repo" ) );
        var testProject = CreateFolder( solution.AppendPart( "P.Tests" ) );

        StaticTestHelper.FindTestsFolder( testProject, solution ).ShouldBeNull();
    }

    [Test]
    public void no_folder_above_a_linked_worktree_is_tried()
    {
        // The worktree is in a "Tests" folder that is in the main checkout: this folder does not count.
        var main = CreateMainCheckout( _root.AppendPart( "Main" ) );
        var worktree = CreateWorktree( main, "wt", main.AppendPart( "Tests" ).AppendPart( "wt" ) );
        var testProject = CreateFolder( worktree.AppendPart( "P.Tests" ) );

        StaticTestHelper.FindTestsFolder( testProject, worktree ).ShouldBeNull();
    }

    [Test]
    public void a_submodule_in_the_Tests_folder_of_its_superproject_uses_this_Tests_folder()
    {
        var super = CreateMainCheckout( _root.AppendPart( "Super" ) );
        var sub = CreateSubmodule( super, "Sub", super.AppendPart( "Tests" ).AppendPart( "Sub" ) );
        var testProject = CreateFolder( sub.AppendPart( "Sub.Tests" ) );

        StaticTestHelper.FindTestsFolder( testProject, sub ).ShouldBe( super.AppendPart( "Tests" ) );
    }

    [Test]
    public void nested_submodules_use_the_Tests_folder_of_the_first_superproject_that_has_one()
    {
        // Super/Tests/A is a submodule of Super, Super/Tests/A/B is a submodule of A.
        var super = CreateMainCheckout( _root.AppendPart( "Super" ) );
        var a = CreateSubmodule( super, "A", super.AppendPart( "Tests" ).AppendPart( "A" ) );
        var b = CreateSubmodule( a, "B", a.AppendPart( "B" ) );
        var testProject = CreateFolder( b.AppendPart( "B.Tests" ) );

        StaticTestHelper.FindTestsFolder( testProject, b ).ShouldBe( super.AppendPart( "Tests" ) );
    }

    [Test]
    public void a_stray_git_file_between_a_submodule_and_its_superproject_is_ignored()
    {
        var super = CreateMainCheckout( _root.AppendPart( "Super" ) );
        var stray = CreateFolder( super.AppendPart( "Tests" ).AppendPart( "Stray" ) );
        File.WriteAllText( stray.AppendPart( ".git" ), "This is not git metadata." );
        var sub = CreateSubmodule( super, "Sub", stray.AppendPart( "Sub" ) );
        var testProject = CreateFolder( sub.AppendPart( "Sub.Tests" ) );

        StaticTestHelper.FindTestsFolder( testProject, sub ).ShouldBe( super.AppendPart( "Tests" ) );
    }

    [Test]
    public void a_Tests_folder_above_the_superproject_is_ignored()
    {
        // Tests/Super is a main checkout, Tests/Super/Libs/Sub is its submodule.
        var super = CreateMainCheckout( _root.AppendPart( "Tests" ).AppendPart( "Super" ) );
        var sub = CreateSubmodule( super, "Sub", super.AppendPart( "Libs" ).AppendPart( "Sub" ) );
        var testProject = CreateFolder( sub.AppendPart( "Sub.Tests" ) );

        StaticTestHelper.FindTestsFolder( testProject, sub ).ShouldBeNull();
    }

    [TestCase( "", true )]
    [TestCase( "/", true )]
    [TestCase( "C:", true )]
    [TestCase( "C:/", true )]
    [TestCase( "//server/share", true )]
    [TestCase( "/src", false )]
    [TestCase( "C:/src", false )]
    [TestCase( "//server/share/src", false )]
    public void IsRootFolder_accepts_only_a_real_root( string path, bool expected )
    {
        StaticTestHelper.IsRootFolder( path ).ShouldBe( expected );
    }

    static NormalizedPath CreateFolder( NormalizedPath folder )
    {
        Directory.CreateDirectory( folder );
        return folder;
    }

    // A main checkout has a ".git" directory.
    static NormalizedPath CreateMainCheckout( NormalizedPath folder )
    {
        Directory.CreateDirectory( folder.AppendPart( ".git" ) );
        File.WriteAllText( folder.AppendPart( ".git" ).AppendPart( "HEAD" ), "ref: refs/heads/main\n" );
        return folder;
    }

    // A submodule has a ".git" file that points to "<super git dir>/modules/<name>": this git directory has a
    // "HEAD" file and no "commondir" file.
    static NormalizedPath CreateSubmodule( NormalizedPath superFolder, string name, NormalizedPath folder )
    {
        var gitDir = GetGitDir( superFolder ).AppendPart( "modules" ).AppendPart( name );
        Directory.CreateDirectory( gitDir );
        File.WriteAllText( gitDir.AppendPart( "HEAD" ), "ref: refs/heads/main\n" );
        Directory.CreateDirectory( folder );
        File.WriteAllText( folder.AppendPart( ".git" ), $"gitdir: {gitDir}\n" );
        return folder;
    }

    // A linked worktree has a ".git" file that points to "<main git dir>/worktrees/<id>": this git directory
    // has a "commondir" file.
    static NormalizedPath CreateWorktree( NormalizedPath mainFolder, string id, NormalizedPath folder )
    {
        var gitDir = mainFolder.AppendPart( ".git" ).AppendPart( "worktrees" ).AppendPart( id );
        Directory.CreateDirectory( gitDir );
        File.WriteAllText( gitDir.AppendPart( "HEAD" ), "ref: refs/heads/wt\n" );
        File.WriteAllText( gitDir.AppendPart( "commondir" ), "../..\n" );
        Directory.CreateDirectory( folder );
        File.WriteAllText( folder.AppendPart( ".git" ), $"gitdir: {gitDir}\n" );
        return folder;
    }

    // The git directory of a main checkout is its ".git" directory. The git directory of a submodule is read
    // from its ".git" file.
    static NormalizedPath GetGitDir( NormalizedPath folder )
    {
        var dotGit = folder.AppendPart( ".git" );
        if( Directory.Exists( dotGit ) ) return dotGit;
        return File.ReadAllText( dotGit ).Substring( "gitdir:".Length ).Trim();
    }
}
