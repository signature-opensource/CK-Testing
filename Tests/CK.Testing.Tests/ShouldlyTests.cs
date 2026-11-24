using CK.Core;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CK.Testing.Tests;

static class ShouldlyTestsExtensions
{
    public static int ThrowingExtension( this string s ) => throw new InvalidOperationException( s );
}

[TestFixture]
public class ShouldlyTests
{
    [Test]
    public void ShouldThrow_on_Delegate_correctly_handles_multicast_Delegate()
    {
        Action bug = () => throw new ArgumentNullException();
        Action noBug = () => { };

        Action? combined = noBug;
        combined += bug;

        combined.ShouldThrow<ArgumentNullException>();
    }

    [Test]
    public void ShouldThrow_on_Func_ValueType_Delegate()
    {
        Func<int> bug = () => throw new ArgumentNullException();
        Func<int> noBug = () => 42;

        Func<int>? combined = noBug;
        combined += bug;

        combined.ShouldThrow<ArgumentNullException>();
    }

    [Test]
    public void ShouldThrow_on_Func_ReferenceType_Delegate()
    {
        Func<string> bug = () => throw new ArgumentNullException();
        Func<string> noBug = () => "Hello";

        Func<string>? combined = noBug;
        combined += bug;

        combined.ShouldThrow<ArgumentNullException>();
    }

    [Test]
    public void ShouldThrow_on_Func_Object_Delegate()
    {
        Func<object> bug = () => throw new ArgumentNullException();
        Func<object> noBug = () => "Hello";

        Func<object>? combined = noBug;
        combined += bug;

        combined.ShouldThrow<ArgumentNullException>();
    }

    [Test]
    public void ShouldThrow_on_Delegate_checks_that_Delegate_has_no_parameter()
    {
        Action<int> expectParameter = SomeFunc;

        Action sut = () => expectParameter.ShouldThrow<ArgumentNullException>();

        sut.ShouldThrow<ArgumentException>().Message.ShouldBe( """
            ShouldThrow can only be called on a delegate without parameters. Delegate type is 'Action<int>'.
            """ );
    }

    static void SomeFunc( int a ) => throw new ArgumentNullException();

    [Test]
    public void ShouldAll_display()
    {
        int[] values = [0, 1, 2];
        Util.Invokable( () => values.ShouldAll( i => i.ShouldBePositive( "Subordinated message." ) ) )
            .ShouldThrow<ShouldAssertException>()
            .Message.ShouldBe( """
            ShouldAll failed for item n°0.
              | Util.Invokable( values.ShouldAll( i => i
              |     should be positive but
              | 0
              |     is negative
              | 
              | Additional Info:
              |     Subordinated message.
            """ );
    }

    [Test]
    public void ShouldMatch_display()
    {
        int v = 0;
        Util.Invokable( () => v.ShouldMatch( i => i > 0, "Custom message." ) )
            .ShouldThrow<ShouldAssertException>()
            .Message.ShouldBe( """
            Util.Invokable( v
                should match
            (i > 0)
                but was
            0

            Additional Info:
                Custom message.
            """ );
    }

    [Test]
    public void ShouldMatch_introduces_no_confilct_resolution()
    {
        "aa".ShouldMatch( "aa" );
        "aa".ShouldMatch( s => s.Length == 2 );
    }

    [Test]
    public void ShouldThrow_on_Delegate_awaits_a_Task_or_a_ValueTask()
    {
        Func<Task> task = async () => { await Task.Yield(); throw new InvalidOperationException(); };
        Func<ValueTask> valueTask = async () => { await Task.Yield(); throw new InvalidOperationException(); };
        Func<ValueTask<int>> valueTaskOfInt = async () => { await Task.Yield(); throw new InvalidOperationException(); };
        ((Delegate)task).ShouldThrow<InvalidOperationException>();
        valueTask.ShouldThrow<InvalidOperationException>();
        valueTaskOfInt.ShouldThrowExactly<InvalidOperationException>();

        Func<ValueTask<int>> noThrow = async () => { await Task.Yield(); return 3712; };
        Util.Invokable( () => noThrow.ShouldThrow<InvalidOperationException>() ).ShouldThrow<ShouldAssertException>();
    }

    [Test]
    public void ShouldThrow_on_Delegate_bound_to_an_extension_method()
    {
        Func<int> bound = "message".ThrowingExtension;
        bound.ShouldThrow<InvalidOperationException>().Message.ShouldBe( "message" );
    }

    [Test]
    public void ShouldBeSubsetOf_uses_the_comparer()
    {
        new[] { "A" }.ShouldBeSubsetOf( ["a", "b"], StringComparer.OrdinalIgnoreCase );
    }

    [Test]
    public void string_comparisons_are_ordinal_by_default()
    {
        Util.Invokable( () => "ABC".ShouldContain( "b" ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "ABC".ShouldStartWith( "a" ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "ABC".ShouldEndWith( "c" ) ).ShouldThrow<ShouldAssertException>();
        "ABC".ShouldNotContain( "b" ).ShouldNotStartWith( "a" ).ShouldNotEndWith( "c" );
        "ABC".ShouldStartWith( "a", StringComparison.OrdinalIgnoreCase ).ShouldEndWith( "c", StringComparison.OrdinalIgnoreCase );

        // Shouldly's Case is supported and the assertions can be chained.
        "ABC".ShouldStartWith( "a", Case.Insensitive )
             .ShouldEndWith( "c", Case.Insensitive )
             .ShouldContain( "b", Case.Insensitive )
             .ShouldNotContain( "b", Case.Sensitive )
             .ShouldNotStartWith( "a", Case.Sensitive )
             .ShouldNotEndWith( "c", Case.Sensitive );
        "ABC".ShouldNotEndWith( "c", "Custom message.", Case.Sensitive );
        Util.Invokable( () => "ABC".ShouldStartWith( "a", Case.Sensitive ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "ABC".ShouldNotEndWith( "c", Case.Insensitive ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "ABC".ShouldContain( "b", Case.Sensitive ) ).ShouldThrow<ShouldAssertException>();

        // The culture orders "a" before "B". The ordinal comparison orders 'B' (66) before 'a' (97).
        new[] { "B", "a" }.ShouldBeInOrder();
        new[] { "a", "B" }.ShouldBeInOrder( SortDirection.Descending );
        "a".ShouldBeGreaterThan( "B" ).ShouldBeGreaterThanOrEqualTo( "a" );
        "B".ShouldBeLessThan( "a" ).ShouldBeLessThanOrEqualTo( "B" );
        "Z".ShouldBeInRange( "A", "a" );
        "b".ShouldNotBeInRange( "A", "a" );
        Util.Invokable( () => new[] { "a", "B" }.ShouldBeInOrder() ).ShouldThrow<ShouldAssertException>();
    }

    [Test]
    public void ShouldBe_on_strings_ignores_the_newline_style()
    {
        "a\r\nb".ShouldBe( "a\nb" );
        "a\nb".ShouldBe( "a\r\nb", "With a message." );
        "a\rb".ShouldBe( "a\nb" );
        "a\r\nb\nc".ShouldBe( "a\nb\r\nc" );
        ((string?)null).ShouldBe( null );
        // The subject is returned as it is, not normalized.
        var s = "a\r\nb";
        ReferenceEquals( s.ShouldBe( "a\nb" ), s ).ShouldBeTrue();
    }

    [Test]
    public void ShouldBe_on_strings_still_fails_on_a_real_difference()
    {
        Util.Invokable( () => "a\r\nb".ShouldBe( "a\nc" ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "a\n".ShouldBe( "a" ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "a\n\nb".ShouldBe( "a\nb" ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "a\u2028b".ShouldBe( "a\nb" ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => ((string?)null).ShouldBe( "a" ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "a".ShouldBe( "b", "The message." ) )
            .ShouldThrow<ShouldAssertException>().Message.ShouldContain( "The message." );
    }

    [Test]
    public void ShouldNotBe_on_strings_ignores_the_newline_style()
    {
        "a\nb".ShouldNotBe( "a\nc" );
        "a".ShouldNotBe( null );
        ((string?)null).ShouldNotBe( "a" );
        // The subject is returned as it is, not normalized.
        var s = "a\r\nb";
        ReferenceEquals( s.ShouldNotBe( "x" ), s ).ShouldBeTrue();
        // Two texts that differ by their newline style only are the same text.
        Util.Invokable( () => "a\r\nb".ShouldNotBe( "a\nb" ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "a\rb".ShouldNotBe( "a\nb", "The message." ) )
            .ShouldThrow<ShouldAssertException>().Message.ShouldContain( "The message." );
        Util.Invokable( () => ((string?)null).ShouldNotBe( null ) ).ShouldThrow<ShouldAssertException>();
        // A comparer makes it exact.
        "a\r\nb".ShouldNotBe( "a\nb", StringComparer.Ordinal );
        Util.Invokable( () => "a\nb".ShouldNotBe( "a\nb", StringComparer.Ordinal ) ).ShouldThrow<ShouldAssertException>();
    }

    [Test]
    public void ShouldNotBe_with_a_StringComparison_is_exact_on_newlines()
    {
        "a\r\nb".ShouldNotBe( "a\nb", StringComparison.Ordinal );
        "a".ShouldNotBe( "b", StringComparison.OrdinalIgnoreCase, "With a message." );
        ((string?)null).ShouldNotBe( "a", StringComparison.Ordinal );
        var s = "a\r\nb";
        ReferenceEquals( s.ShouldNotBe( "a\nb", StringComparison.Ordinal ), s ).ShouldBeTrue();
        Util.Invokable( () => "a\nb".ShouldNotBe( "a\nb", StringComparison.Ordinal ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "a".ShouldNotBe( "A", StringComparison.OrdinalIgnoreCase ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => ((string?)null).ShouldNotBe( null, StringComparison.Ordinal ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "a".ShouldNotBe( "a", StringComparison.Ordinal, "The message." ) )
            .ShouldThrow<ShouldAssertException>().Message.ShouldContain( "The message." );
    }

    [Test]
    public void a_comparer_or_a_comparison_makes_the_assertion_exact()
    {
        "a\nb".ShouldBe( "a\nb", StringComparer.Ordinal );
        Util.Invokable( () => "a\r\nb".ShouldBe( "a\nb", StringComparer.Ordinal ) ).ShouldThrow<ShouldAssertException>();
        "x\r\ny".ShouldContain( "x\r\ny", StringComparison.Ordinal );
        Util.Invokable( () => "x\r\ny".ShouldContain( "x\ny", StringComparison.Ordinal ) ).ShouldThrow<ShouldAssertException>();
        "x\r\ny".ShouldNotContain( "x\ny", StringComparison.Ordinal );
    }

    [Test]
    public void ShouldContain_and_ShouldNotContain_ignore_the_newline_style()
    {
        "0\r\na\r\nb\r\n1".ShouldContain( "a\nb" );
        "0\na\nb\n1".ShouldContain( "a\r\nb", "With a message." );
        "0\ra\rb".ShouldContain( "a\nb" );
        // The subject is returned as it is, not normalized.
        var s = "a\r\nb";
        ReferenceEquals( s.ShouldContain( "a\nb" ), s ).ShouldBeTrue();
        Util.Invokable( () => "0\na\nb".ShouldContain( "a\nc" ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "0\na\u2028b".ShouldContain( "a\nb" ) ).ShouldThrow<ShouldAssertException>();
        "0\na\nb".ShouldNotContain( "a\nc" );
        Util.Invokable( () => "0\r\na\r\nb".ShouldNotContain( "a\nb" ) ).ShouldThrow<ShouldAssertException>();
        // A null subject is a failed assertion, not a NullReferenceException.
        Util.Invokable( () => ((string)null!).ShouldContain( "a" ) ).ShouldThrow<ShouldAssertException>();
        ((string)null!).ShouldNotContain( "a" ).ShouldBeNull();
    }

    [Test]
    public void ShouldBe_with_a_StringComparison_is_exact_on_newlines()
    {
        "a\nb".ShouldBe( "a\nb", StringComparison.Ordinal );
        "a\nB".ShouldBe( "A\nb", StringComparison.OrdinalIgnoreCase, "With a message." );
        ((string?)null).ShouldBe( null, StringComparison.Ordinal );
        var s = "a\r\nb";
        ReferenceEquals( s.ShouldBe( "a\r\nb", StringComparison.Ordinal ), s ).ShouldBeTrue();
        Util.Invokable( () => "a\r\nb".ShouldBe( "a\nb", StringComparison.Ordinal ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "a".ShouldBe( "A", StringComparison.Ordinal ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => ((string?)null).ShouldBe( "a", StringComparison.Ordinal ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => "a".ShouldBe( "b", StringComparison.Ordinal, "The message." ) )
            .ShouldThrow<ShouldAssertException>().Message.ShouldContain( "The message." );
    }

    [Test]
    public void ShouldBe_with_a_StringComparison_shows_where_the_texts_differ()
    {
        // The two texts look the same: Shouldly's difference table gives the code of each character.
        Util.Invokable( () => "a\r\n".ShouldBe( "a\n", StringComparison.Ordinal ) )
            .ShouldThrow<ShouldAssertException>().Message.ShouldContain( "Actual Code" );
        Util.Invokable( () => "ABC".ShouldBe( "abd", StringComparison.OrdinalIgnoreCase ) )
            .ShouldThrow<ShouldAssertException>().Message.ShouldContain( "Ignoring case" );
        // OrdinalIgnoreCase is not a culture comparison (for a culture, "\u00DF" is "ss").
        Util.Invokable( () => "stra\u00DFe".ShouldBe( "STRASSE", StringComparison.OrdinalIgnoreCase ) ).ShouldThrow<ShouldAssertException>();
    }

    [Test]
    public void ShouldBe_on_long_strings_still_shows_the_difference()
    {
        // Shouldly's string message shows the first 5000 characters of each text only.
        var common = new string( 'x', 6000 );
        Util.Invokable( () => (common + "\r\nACTUAL-END").ShouldBe( common + "\nEXPECTED-END" ) )
            .ShouldThrow<ShouldAssertException>().Message.ShouldContain( "ACTUAL-END" ).ShouldContain( "EXPECTED-END" );
        Util.Invokable( () => (common + "ACTUAL-END").ShouldBe( common + "EXPECTED-END", StringComparison.Ordinal ) )
            .ShouldThrow<ShouldAssertException>().Message.ShouldContain( "ACTUAL-END" ).ShouldContain( "EXPECTED-END" );
        (common + "\r\nEND").ShouldBe( common + "\nEND" );
        (common + "\r\nEND").ShouldBe( common + "\r\nEND", StringComparison.Ordinal );
        Util.Invokable( () => (common + "\r\nEND").ShouldBe( common + "\nEND", StringComparison.Ordinal ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => common.ShouldBe( null ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => ((string?)null).ShouldBe( common ) ).ShouldThrow<ShouldAssertException>();
    }

    [Test]
    public void a_string_that_is_not_typed_as_a_string_is_compared_exactly()
    {
        // A limit: only a call that the compiler sees as string.ShouldBe( string ) ignores the newline style.
        object o = "a\r\nb";
        Util.Invokable( () => o.ShouldBe( "a\nb" ) ).ShouldThrow<ShouldAssertException>();
        Util.Invokable( () => new[] { "a\r\nb" }.ShouldBe( new[] { "a\nb" } ) ).ShouldThrow<ShouldAssertException>();
    }

    [Test]
    public void ShouldAll_message_uses_the_newline_of_the_platform()
    {
        int[] values = [0, 1, 2];
        var message = Util.Invokable( () => values.ShouldAll( i => i.ShouldBePositive() ) )
                          .ShouldThrow<ShouldAssertException>().Message;
        message.ReplaceLineEndings().ShouldBe( message, StringComparer.Ordinal );
    }

    [Test]
    public void assertions_that_constrain_their_subject_can_be_chained()
    {
        "Hello World!".ShouldNotBeNullOrWhiteSpace()
                      .ShouldStartWith( "Hello" )
                      .ShouldEndWith( "!" )
                      .ShouldContain( "World" );

        3712.ShouldBePositive()
             .ShouldBeGreaterThan( 3000 )
             .ShouldBeLessThanOrEqualTo( 3712 )
             .ShouldBeInRange( 0, 5000 )
             .ShouldBeOneOf( 3712, 42 );

        Math.PI.ShouldBe( 3.14, 0.01 ).ShouldBeLessThan( 4.0 );
        TimeSpan.FromSeconds( 1 ).ShouldBe( TimeSpan.FromMilliseconds( 999 ), TimeSpan.FromMilliseconds( 10 ) ).ShouldBeGreaterThan( TimeSpan.Zero );

        (AttributeTargets.Class | AttributeTargets.Method).ShouldHaveFlag( AttributeTargets.Class )
                                                         .ShouldNotHaveFlag( AttributeTargets.Field )
                                                         .ShouldBe( AttributeTargets.Class | AttributeTargets.Method );

        var d = new Dictionary<string, int> { { "One", 1 }, { "Two", 2 } };
        d.ShouldContainKey( "One" )
         .ShouldContainKeyAndValue( "Two", 2 )
         .ShouldNotContainKey( "Three" )
         .Count.ShouldBe( 2 );

        new[] { 1, 2, 3 }.ShouldContain( 2 ).ShouldBeInOrder().ShouldBeUnique().ShouldAllBe( i => i > 0 );
    }

    #region Genuine Shouldy tests (no override needed).
    static async Task<int> VTypeAsync( bool error )
    {
        await Task.Delay( 0 );
        if( error ) throw new CKException( "Intentional error." );
        return 3712;
    }

    static async Task<string> RTypeAsync( bool error )
    {
        await Task.Delay( 0 );
        if( error ) throw new CKException( "Intentional error." );
        return "3712";
    }

    [Test]
    public async Task ShouldNotThrowAsync()
    {
        await Util.Awaitable( () => Task.Delay( 15 ) ).ShouldNotThrowAsync();
        await Util.Awaitable( () => RTypeAsync( false ) ).ShouldNotThrowAsync();
        await Util.Awaitable( async () => await VTypeAsync( false ) ).ShouldNotThrowAsync();
    }

    [Test]
    public async Task ShouldThrowAsync()
    {
        (await Util.Awaitable( () => RTypeAsync( true ) ).ShouldThrowAsync<Exception>())
            .Message.ShouldBe( "Intentional error." );
        (await Util.Awaitable( async () => await VTypeAsync( true ) ).ShouldThrowAsync<Exception>())
            .Message.ShouldBe( "Intentional error." );
    }
    #endregion


}
