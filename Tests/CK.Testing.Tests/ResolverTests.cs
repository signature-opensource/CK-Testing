using CK.Core;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;

namespace CK.Testing.Tests;


// A is like Monitor.
public interface IACore : ITestHelperResolvedCallback
{
    bool ResolvedCallbackCalled { get; }
    object ResolvedObject { get; }
    IBasicTestHelper AToBasicRef { get; }
    int CallACount { get; }
    void DoA();
    event EventHandler ADone;
}

public interface IA : IMixinTestHelper, IBasicTestHelper, IACore
{
}

public class A : IACore
{
    readonly IBasicTestHelper _basic;
    int _callCount;
    bool _cbCalled;
    object? _resolvedObject;

    event EventHandler? _aDone;

    internal A( IBasicTestHelper basic )
    {
        _basic = basic;
    }

    int IACore.CallACount => _callCount;

    IBasicTestHelper IACore.AToBasicRef => _basic;

    public bool ResolvedCallbackCalled => _cbCalled;

    public object ResolvedObject => _resolvedObject!;

    void IACore.DoA()
    {
        _basic.BuildConfiguration.ShouldBeOneOf( ["Debug", "Release"] );
        _basic.TestProjectName.ShouldBe( "CK.Testing.Tests" );
        ++_callCount;
        _aDone?.Invoke( this, EventArgs.Empty );
    }

    void ITestHelperResolvedCallback.OnTestHelperGraphResolved( object resolvedObject )
    {
        _cbCalled.ShouldBeFalse();
        _cbCalled = true;
        _resolvedObject = resolvedObject;
    }

    event EventHandler? IACore.ADone
    {
        add => _aDone += value;
        remove => _aDone -= value;
    }

}

// B is like SqlTransform.
public interface IBCore
{
    IA BToARef { get; }
    IBasicTestHelper BToBasicRef { get; }
    void DoB();
}

public interface IB : IMixinTestHelper, IA, IBCore
{
}

public class B : IBCore
{
    readonly TestHelperConfiguration _config;
    readonly IA _a;
    readonly IBasicTestHelper _basic;

    internal B( TestHelperConfiguration config, IA a, IBasicTestHelper basic )
    {
        _config = config;
        _a = a;
        _basic = basic;
    }

    IA IBCore.BToARef => _a;

    IBasicTestHelper IBCore.BToBasicRef => _basic;

    void IBCore.DoB() { }
}

// C is like StObjMap
public interface ICCore
{
    IA CToARef { get; }
    IBasicTestHelper CToBasicRef { get; }
    void DoC();
}

public interface IC : IMixinTestHelper, IA, ICCore
{
}

public class C : ICCore
{
    readonly IBasicTestHelper _basic;
    readonly IA _a;

    public C( IA a, IBasicTestHelper basic )
    {
        _a = a;
        _basic = basic;
    }

    IA ICCore.CToARef => _a;

    IBasicTestHelper ICCore.CToBasicRef => _basic;

    void ICCore.DoC()
    {
    }
}

// D is like CKSetup
public interface IDCore
{
    void DoD();
}

public interface ID : IMixinTestHelper, IA, IDCore
{
}

public class D : IDCore
{
    readonly IA _a;

    public D( IA a )
    {
        _a = a;
    }

    void IDCore.DoD()
    {
    }
}

// E is like SqlServer
public interface IECore
{
    void EoE();
}

public interface IE : IMixinTestHelper, IA, IECore
{
}

public class E : IECore
{
    readonly IA _a;

    public E( IA a )
    {
        _a = a;
    }

    void IECore.EoE()
    {
    }
}

// F is like DBSetup.
public interface IFCore
{
    void DoF();
}

public interface IF : IMixinTestHelper, IE, ID, IC
{
}

class F : IFCore
{
    readonly ID _d;
    readonly IE _e;
    readonly IC _c;

    public F( TestHelperConfiguration config, ID d, IE e, IC c )
    {
        _d = d;
        _e = e;
        _c = c;
    }

    void IFCore.DoF()
    {
    }
}

public interface INotImplCore
{
    void Do();
}

public interface INotImpl : IMixinTestHelper, IA, INotImplCore
{
}


[TestFixture]
public class ResolverTests
{
    [Test]
    public void configuration_values_Declared_and_Useless()
    {
        var config = new TestHelperConfiguration();

        string editableValue = "I can change!";

        var pathsAreOptionals = config.Declare( "Anything/nawak/Deep/Ambiguous", "D0", null );
        pathsAreOptionals.ConfiguredValue.ShouldBe( "I'm at the root!" );

        var usedAndConfigured = config.DeclareMultiPaths( "Test/MultiPaths", "D1", null ).Value.ToList();
        // A Value not editable AND without default should not exist but this is not checked (its CurrentValue is simply null).
        var unconfiguredWithDefault = config.Declare( "Test/UnconfWithDefault", "the default value", "D2", null );
        var unconfiguredEditable = config.Declare( "Test/UnconfWithEditableValue", "D3", () => editableValue );
        //
        var renamed = config.Declare( "Thing/NewName", "Deprecated name", null, "OldThing/OldName" );
        renamed.Key.Path.ShouldBe( "Thing/NewName" );
        renamed.ConfiguredValue.ShouldBe( "I'm using a deprecated name." );
        renamed.ObsoleteKeyUsed.ShouldBe( "OldName" );


        config.UselessValues.Count().ShouldBe( 1 );
        config.UselessValues.ShouldContain( x => x.UnusedKey.Path == "Test/UnusedKey" && x.ConfiguredValue == "unused" );
        config.DeclaredValues.Count().ShouldBe( 5 );

        // MultiStrings are trimmed but duplicates ';;;;' appear in the ConfiguredValue so that user can see them
        // (the default value is set (if not editable) to the evaluated paths (not tested here).
        config.DeclaredValues.ShouldContain( x => x.Key.Path == "Test/MultiPaths"
                                                  && x.Description == "D1"
                                                  && x.ConfiguredValue == @"{SolutionFolder}..;{TestProjectFolder}/../XXXXX/{BuildConfiguration};;X/../{TestProjectName};{ClosestSUTProjectFolder}/{BuildConfiguration}-{TestProjectName}-{SolutionName}/{PathToBin};../Y;{X}\{BuildConfiguration}\..\Y;;;;;;" );

        config.DeclaredValues.ShouldContain( x => x.Key.Path == "Test/UnconfWithDefault"
                                                  && x.Description == "D2"
                                                  && x.ConfiguredValue == null
                                                  && x.CurrentValue == "the default value" );

        config.DeclaredValues.ShouldContain( x => x.Key.Path == "Test/UnconfWithEditableValue"
                                                  && x.Description == "D3"
                                                  && x.ConfiguredValue == null
                                                  && x.CurrentValue == "I can change!" );

        config.DeclaredValues.ShouldContain( renamed );
        config.DeclaredValues.ShouldContain( pathsAreOptionals );
    }

    [TestCase( "ConfigurationResolvedFirst" )]
    [TestCase( "BasicTestHelperResolvedFirst" )]
    public void configuration_value_as_paths( string mode )
    {

        IBasicTestHelper b;
        TestHelperConfiguration config;

        {
            var resolver = TestHelperResolver.Create( new TestHelperConfiguration() );
            if( mode == "BasicTestHelperResolvedFirst" )
            {
                b = resolver.Resolve<IBasicTestHelper>();
                config = resolver.Resolve<TestHelperConfiguration>();
            }
            else
            {
                config = resolver.Resolve<TestHelperConfiguration>();
                b = resolver.Resolve<IBasicTestHelper>();
            }
        }

        // In a git worktree, the SolutionFolder can have any name, but the SolutionName is the name of the main repository.
        b.SolutionName.ShouldBe( "CK-Testing" );
        b.TestProjectName.ShouldBe( "CK.Testing.Tests" );
        b.SolutionFolder.ShouldBe( b.TestProjectFolder.RemoveLastPart().RemoveLastPart(), "This test project is <SolutionFolder>/Tests/CK.Testing.Tests." );
        File.Exists( b.SolutionFolder.AppendPart( "CK-Testing.slnx" ) ).ShouldBeTrue();
        var dotGit = b.SolutionFolder.AppendPart( ".git" );
        (Directory.Exists( dotGit ) || File.Exists( dotGit )).ShouldBeTrue( "A main checkout has a .git directory, a git worktree has a .git file." );
        b.ClosestSUTProjectFolder.ShouldBe( b.SolutionFolder.AppendPart( "CK.Testing" ) );

        var paths = config.DeclareMultiPaths( "Test/MultiPaths", "The description of MultiPaths.", null ).Value.ToList();
        paths.Count.ShouldBe( 6 );

        paths[0].ShouldBe( new NormalizedPath( Path.GetDirectoryName( b.SolutionFolder ) ), "{SolutionFolder}.." );

        paths[1].ShouldBe( b.TestProjectFolder.RemoveLastPart().AppendPart( "XXXXX" ).AppendPart( b.BuildConfiguration ), "{TestProjectFolder}/../XXXXX/{BuildConfiguration}" );

        paths[2].ShouldBe( b.TestProjectFolder.AppendPart( b.TestProjectName ), "X/../{TestProjectName}" );

        var expectedPlacehoders = $"{b.BuildConfiguration}-{b.TestProjectName}-{b.SolutionName}";
        paths[3].ShouldBe( b.ClosestSUTProjectFolder.AppendPart( expectedPlacehoders ).Combine( b.PathToBin ),
            "{ClosestSUTProjectFolder}/{BuildConfiguration}-{TestProjectName}-{SolutionName}/{PathToBin}" );

        paths[4].ShouldBe( b.TestProjectFolder.RemoveLastPart().AppendPart( "Y" ), "../Y" );

        // No .. resolved when { appears.
        paths[5].ShouldBe( $"{{X}}/{b.BuildConfiguration}/../Y", "Since a { exists, the .. are not resolved. {X}/{BuildConfiguration}/../Y" );
        paths[5].ResolveDots().ShouldBe( $"{{X}}/Y" );
    }

    [Test]
    public void resolving_one_simple_mixin_as_singleton()
    {
        ITestHelperResolver resolver = TestHelperResolver.Create( new TestHelperConfiguration() );
        int eventCount = 0;
        var a = resolver.Resolve<IA>();
        a.ADone += ( e, arg ) => ++eventCount;
        a.ResolvedCallbackCalled.ShouldBeTrue();
        a.ResolvedObject.ShouldBeAssignableTo<IA>( "The resolved object here is the Mixin." );

        int originalCount = a.CallACount;
        a.DoA();
        a.CallACount.ShouldBe( originalCount + 1 );

        var a2 = resolver.Resolve<IACore>();
        a2.ShouldNotBeSameAs( a, "Accessing IACore returns the A implementation object." );
        a2.DoA();
        a.CallACount.ShouldBe( originalCount + 2 );
    }

    [Test]
    public void resolving_one_core_as_singleton()
    {
        ITestHelperResolver resolver = TestHelperResolver.Create( new TestHelperConfiguration() );
        int eventCount = 0;
        var a = resolver.Resolve<IACore>();
        a.ADone += ( e, arg ) => ++eventCount;
        a.ResolvedCallbackCalled.ShouldBeTrue();
        a.ResolvedObject.ShouldBeAssignableTo<A>( "The resolved object here is NOT a Mixin, It is just the A implementation." );

        int originalCount = a.CallACount;
        a.DoA();
        a.CallACount.ShouldBe( originalCount + 1 );

        var a2 = resolver.Resolve<IA>();
        a2.ShouldNotBeSameAs( a, "This is the Mixin built on A." );

        a2.DoA();
        a.CallACount.ShouldBe( originalCount + 2 );
    }

    [Test]
    public void when_the_Core_implementation_is_not_found_an_exception_is_raised()
    {
        Action sut = () => TestHelperResolver.Default.Resolve<INotImpl>();
        sut.ShouldThrow<Exception>().Message.ShouldStartWith( "Unable to locate an implementation for " );
    }

    [TestCase( true )]
    [TestCase( false )]
    public void accessing_mixins_as_singleton( bool revert )
    {
        var r = TestHelperResolver.Create( new TestHelperConfiguration() );

        IBasicTestHelper basic;
        IA a;
        IB b;
        IC c;
        ID d;
        IE e;
        IF f;
        if( revert )
        {
            f = r.Resolve<IF>();
            e = r.Resolve<IE>();
            d = r.Resolve<ID>();
            c = r.Resolve<IC>();
            b = r.Resolve<IB>();
            a = r.Resolve<IA>();
            basic = r.Resolve<IBasicTestHelper>();
        }
        else
        {
            basic = r.Resolve<IBasicTestHelper>();
            a = r.Resolve<IA>();
            b = r.Resolve<IB>();
            c = r.Resolve<IC>();
            d = r.Resolve<ID>();
            e = r.Resolve<IE>();
            f = r.Resolve<IF>();
        }
        a.ResolvedCallbackCalled.ShouldBeTrue();
        a.AToBasicRef.ShouldBeSameAs( basic );
        b.BToARef.ShouldBeSameAs( a );
        c.CToARef.ShouldBeSameAs( a );
        c.CToBasicRef.ShouldBeSameAs( basic );
    }

}
