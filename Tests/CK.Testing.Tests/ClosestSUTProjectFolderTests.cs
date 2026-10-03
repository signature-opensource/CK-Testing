using CK.Core;
using NUnit.Framework;
using Shouldly;
using System;
using System.Diagnostics;
using System.Linq;

namespace CK.Testing.Tests;


[TestFixture]
public class ClosestSUTProjectFolderTests
{
    [Test]
    public void find_next_to_the_test()
    {
        var directories = new[]
        {
            "X:/S/Tests/P.Tests",
            "X:/S/Tests/P"
        };
        FindClosestSUTProject( "X:/S/Tests/P.Tests", p => directories.AsSpan().Contains( p ) ).ShouldBe( "X:/S/Tests/P" );
    }

    [Test]
    public void find_above_the_test()
    {
        var directories = new[]
        {
            "X:/S/Tests/P.Tests",
            "X:/S/P"
        };
        FindClosestSUTProject( "X:/S/Tests/P.Tests", p => directories.AsSpan().Contains( p ) ).ShouldBe( "X:/S/P" );
    }

    [Test]
    public void a_parent_folder_cannot_be_the_SUT_1()
    {
        var directories = new[]
        {
            "X:/S/P/Tests/P.Tests",
            "X:/S"
        };
        FindClosestSUTProject( "X:/S/P/Tests/P.Tests", p => directories.AsSpan().Contains( p ) ).ShouldBe( new NormalizedPath() );
    }

    [Test]
    public void a_parent_folder_cannot_be_the_SUT_2()
    {
        var directories = new[]
        {
            "X:/S/P/Tests/P.Tests",
            "X:/S/P"
        };
        FindClosestSUTProject( "X:/S/P/Tests/P.Tests", p => directories.AsSpan().Contains( p ) ).ShouldBe( new NormalizedPath() );
    }

    [Test]
    public void a_parent_folder_cannot_be_the_SUT_3()
    {
        var directories = new[]
        {
            "X:/S/P/Tests/P.Tests",
            "X:/S/P/Tests"
        };
        FindClosestSUTProject( "X:/S/P/Tests/P.Tests", p => directories.AsSpan().Contains( p ) ).ShouldBe( new NormalizedPath() );
    }

    [Test]
    public void find_above_even_if_test_is_in_a_sub_path()
    {
        var directories = new[]
        {
            "X:/S/Tests/Sub/P.Tests",
            "X:/S/P"
        };
        FindClosestSUTProject( "X:/S/Tests/Sub/P.Tests", p => directories.AsSpan().Contains( p ) ).ShouldBe( "X:/S/P" );
    }

    [Test]
    public void find_above_with_a_same_sub_path()
    {
        var directories = new[]
        {
            "X:/S/Tests/Sub/P.Tests",
            "X:/S/P",
            // Won't find this one:
            "X:/S/Sub/P"
        };
        FindClosestSUTProject( "X:/S/Tests/Sub/P.Tests", p => directories.AsSpan().Contains( p ) ).ShouldBe( "X:/S/P" );
    }

    [Test]
    public void find_above_with_a_same_sub_path_in_a_root_Tests_folder()
    {
        var directories = new[]
        {
                "X:/S/Tests/Lot/Tests/Sub/P.Tests",
                "X:/S/Tests/Lot/P",
                // Won't find these ones.
                "X:/S/Tests/Lot/Sub/P",
                "X:/S/Tests/Sub/P",
                "X:/S/Tests/P"
            };
        FindClosestSUTProject( "X:/S/Tests/Lot/Tests/Sub/P.Tests", p => directories.AsSpan().Contains( p ) ).ShouldBe( "X:/S/Tests/Lot/P" );
    }

    [Test]
    public void find_above_with_a_same_sub_path_in_a_root_Tests_folder2()
    {
        var directories = new[]
        {
            "X:/S/A/B/C/D/E/F/G/P.Tests",
            "X:/S/A/B/P",
            // Won't find these ones.
            "X:/S/A/B/D/P",
            "X:/S/A/B/D/E/P",
            "X:/S/A/B/F/G/P",
            "X:/S/A/B/E/P"
        };
        FindClosestSUTProject( "X:/S/A/B/C/D/E/F/G/P.Tests", p => directories.AsSpan().Contains( p ) )
            .ShouldBe( "X:/S/A/B/P" );
    }


    [Test]
    public void SUT_project_is_always_preferred()
    {
        var directories = new[]
        {
            "X:/S/Any/Tests/Somewhere/Tests/Lot/Tests/Sub/P.Tests",
            "X:/S/P.SUT",
            // Won't find these ones.
            "X:/S/Any/Tests/Somewhere/Tests/Lot/Tests/Sub/P",
            "X:/S/Tests/Lot/Sub/P",
            "X:/S/Tests/Lot/P",
            "X:/S/Tests/Sub/P",
            "X:/S/Tests/P"
        };
        FindClosestSUTProject( "X:/S/Any/Tests/Somewhere/Tests/Lot/Tests/Sub/P.Tests", p => directories.AsSpan().Contains( p ) )
            .ShouldBe( "X:/S/P.SUT" );
    }

    [Test]
    public void SUT_project_is_always_preferred2()
    {
        var directories = new[]
        {
            "X:/S/Any/Tests/Somewhere/Tests/Lot/Tests/Sub/P.Tests",
            "X:/S/Tests/Somewhere/Tests/Lot/Tests/Sub/P.SUT",
            // Won't find these ones.
            "X:/S/Any/Tests/Somewhere/Tests/Lot/Tests/Sub/P",
            "X:/S/Tests/Lot/Sub/P",
            "X:/S/Tests/Lot/P",
            "X:/S/Tests/Sub/P",
            "X:/S/Tests/P"
        };
        FindClosestSUTProject( "X:/S/Any/Tests/Somewhere/Tests/Lot/Tests/Sub/P.Tests", p => directories.AsSpan().Contains( p ) )
            .ShouldBe( "X:/S/Tests/Somewhere/Tests/Lot/Tests/Sub/P.SUT" );
    }

    static NormalizedPath FindClosestSUTProject( NormalizedPath testProjectFolder, Func<NormalizedPath, bool> exists )
    {
        Debug.Assert( exists( testProjectFolder ) );
        var candidates = StaticTestHelper.GetClosestSUTProjectCandidatePaths( "X:/S", testProjectFolder ).ToArray();
        return candidates.Where( exists ).FirstOrDefault();
    }


}
