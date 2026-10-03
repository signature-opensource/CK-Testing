using CK.Core;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// This class is exceptionally defined in the global namespace: an extension method of the global namespace is
/// chosen before an extension method of a namespace imported by a using directive (like <c>using Shouldly;</c>).
/// An extension method of a namespace that contains the calling code is still chosen before it.
/// <para>
/// This is bad and must remain exceptional. But, here, it enables to use Shouldly and "override" some
/// of its definitions:
/// <list type="bullet">
///     <item>
///     Strings are compared with <see cref="StringComparison.Ordinal"/> by default: Shouldly ignores the case to find a
///     substring, a start or an end, and orders strings with the current culture.
///     </item>
///     <item>A regular expression is <see cref="RegexOptions.Singleline"/> by default and accepts other options.</item>
///     <item>
///     An assertion returns its subject, so that assertions can be chained. An assertion that fully defines its subject
///     (like <c>ShouldBeNull</c>, <c>ShouldBeTrue</c> or <c>ShouldBeEmpty</c>) is not overridden.
///     </item>
/// </list>
/// </para>
/// </summary>
#pragma warning disable CA1050 // Declare types in namespaces
[DebuggerStepThrough]
[ShouldlyMethods]
[EditorBrowsable( EditorBrowsableState.Never )]
public static class CKShouldlyGlobalOverrideExtensions
{
    #region Strings: ordinal comparison and regular expressions.

    /// <summary>
    /// Explicit overload that avoid to consider a string as a enumerable of characters and support
    /// default <see cref="StringComparison.Ordinal"/>.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The substring that must be found.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string ShouldContain( this string actual, ReadOnlySpan<char> expected, string? customMessage = null )
    {
        return ShouldContain( actual, expected, StringComparison.Ordinal, customMessage );
    }

    /// <summary>
    /// Explicit overload that avoid to consider a string as a enumerable of characters and support
    /// standard <see cref="StringComparison"/> (instead of <see cref="Case"/>).
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The substring that must be found.</param>
    /// <param name="comparisonType">Specifies the type of comparison.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static string ShouldContain( this string actual, ReadOnlySpan<char> expected, StringComparison comparisonType, string? customMessage = null )
    {
        if( !actual.AsSpan().Contains( expected, comparisonType ) )
            throw new ShouldAssertException( new ExpectedActualShouldlyMessage( new string( expected ), actual, customMessage ).ToString() );
        return actual;
    }

    /// <summary>
    /// Explicit overload that avoid to consider a string as a enumerable of characters and support
    /// default <see cref="StringComparison.Ordinal"/>.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The substring that must not be found.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string ShouldNotContain( this string actual,
                                           ReadOnlySpan<char> expected,
                                           string? customMessage = null )
    {
        return ShouldNotContain( actual, expected, StringComparison.Ordinal, customMessage );
    }

    /// <summary>
    /// Explicit overload that avoid to consider a string as a enumerable of characters and support
    /// standard <see cref="StringComparison"/> (instead of <see cref="Case"/>).
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The substring that must not be found.</param>
    /// <param name="comparisonType">Specifies the type of comparison.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static string ShouldNotContain( this string actual,
                                           ReadOnlySpan<char> expected,
                                           StringComparison comparisonType,
                                           string? customMessage = null )
    {
        if( actual.AsSpan().Contains( expected, comparisonType ) )
            throw new ShouldAssertException( new ExpectedActualShouldlyMessage( new string( expected ), actual, customMessage ).ToString() );
        return actual;
    }

    /// <summary>
    /// Override ShouldMatch to support Regex syntax on the string with the default options:
    /// <see cref="RegexOptions.Singleline"/> | <see cref="RegexOptions.ExplicitCapture"/> | <see cref="RegexOptions.CultureInvariant"/>.
    /// </summary>
    /// <param name="actual">This string to match.</param>
    /// <param name="pattern">The expected pattern.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string ShouldMatch( this string actual,
                                      [StringSyntax( "Regex" )] string pattern,
                                      string? customMessage = null )
    {
        return ShouldMatch( actual, pattern, RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, customMessage );
    }

    /// <summary>
    /// Override ShouldMatch to support Regex syntax on the string and <paramref name="options"/>.
    /// </summary>
    /// <param name="actual">This string to match.</param>
    /// <param name="pattern">The expected pattern.</param>
    /// <param name="options">The options.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static string ShouldMatch( this string actual,
                                      [StringSyntax( "Regex" )] string pattern,
                                      RegexOptions options,
                                      string? customMessage = null )
    {
        actual.AssertAwesomely( v => Regex.IsMatch( actual, pattern, options ), actual, pattern, customMessage );
        return actual;
    }

    /// <summary>
    /// Override ShouldNotMatch to support Regex syntax on the string with the default options:
    /// <see cref="RegexOptions.Singleline"/> | <see cref="RegexOptions.ExplicitCapture"/> | <see cref="RegexOptions.CultureInvariant"/>.
    /// </summary>
    /// <param name="actual">This string to match.</param>
    /// <param name="pattern">The pattern that must not match.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string ShouldNotMatch( this string actual,
                                         [StringSyntax( "Regex" )] string pattern,
                                         string? customMessage = null )
    {
        return ShouldNotMatch( actual, pattern, RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, customMessage );
    }

    /// <summary>
    /// Override ShouldNotMatch to support Regex syntax on the string and <paramref name="options"/>.
    /// </summary>
    /// <param name="actual">This string to match.</param>
    /// <param name="pattern">The pattern that must not match.</param>
    /// <param name="options">The options.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static string ShouldNotMatch( this string actual,
                                         [StringSyntax( "Regex" )] string pattern,
                                         RegexOptions options,
                                         string? customMessage = null )
    {
        actual.AssertAwesomely( v => !Regex.IsMatch( actual, pattern, options ), actual, pattern, customMessage );
        return actual;
    }

    #endregion

    #region Strings: chaining.

    /// <summary>
    /// Override ShouldStartWith to use the default <see cref="StringComparison.Ordinal"/>.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The expected start.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string ShouldStartWith( [NotNull] this string? actual, string expected, string? customMessage = null )
    {
        return ShouldStartWith( actual, expected, StringComparison.Ordinal, customMessage );
    }

    /// <summary>
    /// Override ShouldStartWith to support standard <see cref="StringComparison"/> (instead of <see cref="Case"/>).
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The expected start.</param>
    /// <param name="comparisonType">Specifies the type of comparison.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static string ShouldStartWith( [NotNull] this string? actual, string expected, StringComparison comparisonType, string? customMessage = null )
    {
        if( actual == null || !actual.StartsWith( expected, comparisonType ) )
            throw new ShouldAssertException( new ExpectedActualShouldlyMessage( expected, actual, customMessage ).ToString() );
        return actual;
    }

    /// <summary>
    /// Override ShouldNotStartWith to use the default <see cref="StringComparison.Ordinal"/>.
    /// A null string does not start with anything.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The start that must not be found.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string? ShouldNotStartWith( this string? actual, string expected, string? customMessage = null )
    {
        return ShouldNotStartWith( actual, expected, StringComparison.Ordinal, customMessage );
    }

    /// <summary>
    /// Override ShouldNotStartWith to support standard <see cref="StringComparison"/> (instead of <see cref="Case"/>).
    /// A null string does not start with anything.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The start that must not be found.</param>
    /// <param name="comparisonType">Specifies the type of comparison.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static string? ShouldNotStartWith( this string? actual, string expected, StringComparison comparisonType, string? customMessage = null )
    {
        if( actual != null && actual.StartsWith( expected, comparisonType ) )
            throw new ShouldAssertException( new ExpectedActualShouldlyMessage( expected, actual, customMessage ).ToString() );
        return actual;
    }

    /// <summary>
    /// Override ShouldEndWith to use the default <see cref="StringComparison.Ordinal"/>.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The expected end.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string ShouldEndWith( [NotNull] this string? actual, string expected, string? customMessage = null )
    {
        return ShouldEndWith( actual, expected, StringComparison.Ordinal, customMessage );
    }

    /// <summary>
    /// Override ShouldEndWith to support standard <see cref="StringComparison"/> (instead of <see cref="Case"/>).
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The expected end.</param>
    /// <param name="comparisonType">Specifies the type of comparison.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static string ShouldEndWith( [NotNull] this string? actual, string expected, StringComparison comparisonType, string? customMessage = null )
    {
        if( actual == null || !actual.EndsWith( expected, comparisonType ) )
            throw new ShouldAssertException( new ExpectedActualShouldlyMessage( expected, actual, customMessage ).ToString() );
        return actual;
    }

    /// <summary>
    /// Override ShouldNotEndWith to use the default <see cref="StringComparison.Ordinal"/>.
    /// A null string does not end with anything.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The end that must not be found.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string? ShouldNotEndWith( this string? actual, string expected, string? customMessage = null )
    {
        return ShouldNotEndWith( actual, expected, StringComparison.Ordinal, customMessage );
    }

    /// <summary>
    /// Override ShouldNotEndWith to support standard <see cref="StringComparison"/> (instead of <see cref="Case"/>).
    /// A null string does not end with anything.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The end that must not be found.</param>
    /// <param name="comparisonType">Specifies the type of comparison.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static string? ShouldNotEndWith( this string? actual, string expected, StringComparison comparisonType, string? customMessage = null )
    {
        if( actual != null && actual.EndsWith( expected, comparisonType ) )
            throw new ShouldAssertException( new ExpectedActualShouldlyMessage( expected, actual, customMessage ).ToString() );
        return actual;
    }

    /// <summary>
    /// Override ShouldContain with Shouldly's <see cref="Case"/>: <see cref="Case.Sensitive"/> is <see cref="StringComparison.Ordinal"/>,
    /// <see cref="Case.Insensitive"/> is <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The substring that must be found.</param>
    /// <param name="caseSensitivity">The case sensitivity.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string ShouldContain( this string actual, ReadOnlySpan<char> expected, Case caseSensitivity, string? customMessage = null )
    {
        return ShouldContain( actual, expected, ToComparison( caseSensitivity ), customMessage );
    }

    /// <summary>
    /// Override ShouldNotContain with Shouldly's <see cref="Case"/>: <see cref="Case.Sensitive"/> is <see cref="StringComparison.Ordinal"/>,
    /// <see cref="Case.Insensitive"/> is <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The substring that must not be found.</param>
    /// <param name="caseSensitivity">The case sensitivity.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string ShouldNotContain( this string actual, ReadOnlySpan<char> expected, Case caseSensitivity, string? customMessage = null )
    {
        return ShouldNotContain( actual, expected, ToComparison( caseSensitivity ), customMessage );
    }

    /// <summary>
    /// Override ShouldStartWith with Shouldly's <see cref="Case"/>: <see cref="Case.Sensitive"/> is <see cref="StringComparison.Ordinal"/>,
    /// <see cref="Case.Insensitive"/> is <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The expected start.</param>
    /// <param name="caseSensitivity">The case sensitivity.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string ShouldStartWith( [NotNull] this string? actual, string expected, Case caseSensitivity, string? customMessage = null )
    {
        return ShouldStartWith( actual, expected, ToComparison( caseSensitivity ), customMessage );
    }

    /// <summary>
    /// Override ShouldNotStartWith with Shouldly's <see cref="Case"/>: <see cref="Case.Sensitive"/> is <see cref="StringComparison.Ordinal"/>,
    /// <see cref="Case.Insensitive"/> is <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The start that must not be found.</param>
    /// <param name="caseSensitivity">The case sensitivity.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string? ShouldNotStartWith( this string? actual, string expected, Case caseSensitivity, string? customMessage = null )
    {
        return ShouldNotStartWith( actual, expected, ToComparison( caseSensitivity ), customMessage );
    }

    /// <summary>
    /// Override ShouldEndWith with Shouldly's <see cref="Case"/>: <see cref="Case.Sensitive"/> is <see cref="StringComparison.Ordinal"/>,
    /// <see cref="Case.Insensitive"/> is <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The expected end.</param>
    /// <param name="caseSensitivity">The case sensitivity.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string ShouldEndWith( [NotNull] this string? actual, string expected, Case caseSensitivity, string? customMessage = null )
    {
        return ShouldEndWith( actual, expected, ToComparison( caseSensitivity ), customMessage );
    }

    /// <summary>
    /// Override ShouldNotEndWith with Shouldly's <see cref="Case"/>: <see cref="Case.Sensitive"/> is <see cref="StringComparison.Ordinal"/>,
    /// <see cref="Case.Insensitive"/> is <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The end that must not be found.</param>
    /// <param name="caseSensitivity">The case sensitivity.</param>
    /// <param name="customMessage">Optional message.</param>
    /// <returns>This string.</returns>
    public static string? ShouldNotEndWith( this string? actual, string expected, Case caseSensitivity, string? customMessage = null )
    {
        return ShouldNotEndWith( actual, expected, ToComparison( caseSensitivity ), customMessage );
    }

    /// <summary>
    /// Override ShouldNotEndWith with Shouldly's <see cref="Case"/> after the message (the order of a Shouldly overload).
    /// </summary>
    /// <param name="actual">This string.</param>
    /// <param name="expected">The end that must not be found.</param>
    /// <param name="customMessage">The message.</param>
    /// <param name="caseSensitivity">The case sensitivity.</param>
    /// <returns>This string.</returns>
    public static string? ShouldNotEndWith( this string? actual, string expected, string? customMessage, Case caseSensitivity )
    {
        return ShouldNotEndWith( actual, expected, ToComparison( caseSensitivity ), customMessage );
    }

    static StringComparison ToComparison( Case caseSensitivity ) => caseSensitivity == Case.Sensitive
                                                                        ? StringComparison.Ordinal
                                                                        : StringComparison.OrdinalIgnoreCase;

    /// <summary>Chaining <c>ShouldNotBeNullOrEmpty</c>: returns this string.</summary>
    public static string ShouldNotBeNullOrEmpty( [NotNull] this string? actual, string? customMessage = null )
    {
        Shouldly.ShouldBeStringTestExtensions.ShouldNotBeNullOrEmpty( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotBeNullOrWhiteSpace</c>: returns this string.</summary>
    public static string ShouldNotBeNullOrWhiteSpace( [NotNull] this string? actual, string? customMessage = null )
    {
        Shouldly.ShouldBeStringTestExtensions.ShouldNotBeNullOrWhiteSpace( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldContainWithoutWhitespace</c>: returns this string.</summary>
    public static string ShouldContainWithoutWhitespace( this string actual, object? expected, string? customMessage = null )
    {
        Shouldly.ShouldBeStringTestExtensions.ShouldContainWithoutWhitespace( actual, expected, customMessage );
        return actual;
    }

    #endregion

    #region Enumerables: chaining.

    /// <summary>Chaining <c>ShouldContain</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldContain<T>( this IEnumerable<T> actual, T expected, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldContain( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldContain</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldContain<T>( this IEnumerable<T> actual, T expected, IEqualityComparer<T> comparer, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldContain( actual, expected, comparer, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotContain</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldNotContain<T>( this IEnumerable<T> actual, T expected, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldNotContain( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotContain</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldNotContain<T>( this IEnumerable<T> actual, T expected, IEqualityComparer<T> comparer, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldNotContain( actual, expected, comparer, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldContain</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldContain<T>( this IEnumerable<T> actual, Expression<Func<T, bool>> elementPredicate, int expectedCount, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldContain( actual, elementPredicate, expectedCount, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldContain</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldContain<T>( this IEnumerable<T> actual, Expression<Func<T, bool>> elementPredicate, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldContain( actual, elementPredicate, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotContain</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldNotContain<T>( this IEnumerable<T> actual, Expression<Func<T, bool>> elementPredicate, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldNotContain( actual, elementPredicate, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldAllBe</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldAllBe<T>( this IEnumerable<T> actual, Expression<Func<T, bool>> elementPredicate, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldAllBe( actual, elementPredicate, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotBeEmpty</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldNotBeEmpty<T>( [NotNull] this IEnumerable<T>? actual, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldNotBeEmpty( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldContain</c>: returns this enumerable.</summary>
    public static IEnumerable<float> ShouldContain( this IEnumerable<float> actual, float expected, double tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldContain( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldContain</c>: returns this enumerable.</summary>
    public static IEnumerable<double> ShouldContain( this IEnumerable<double> actual, double expected, double tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldContain( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeSubsetOf</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldBeSubsetOf<T>( this IEnumerable<T> actual, IEnumerable<T> expected, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldBeSubsetOf( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeSubsetOf</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldBeSubsetOf<T>( this IEnumerable<T> actual, IEnumerable<T> expected, IEqualityComparer<T> comparer, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldBeSubsetOf( actual, expected, comparer, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeUnique</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldBeUnique<T>( this IEnumerable<T> actual, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldBeUnique( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeUnique</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldBeUnique<T>( this IEnumerable<T> actual, IEqualityComparer<T> comparer, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldBeUnique( actual, comparer, customMessage! );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c>: returns this enumerable.</summary>
    public static IEnumerable<string> ShouldBe( this IEnumerable<string> actual, IEnumerable<string> expected, Case caseSensitivity, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldBe( actual, expected, caseSensitivity, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeInOrder</c>: returns this enumerable. Without comparer, strings are compared with <see cref="StringComparer.Ordinal"/>.</summary>
    public static IEnumerable<T> ShouldBeInOrder<T>( this IEnumerable<T> actual, string? customMessage = null )
    {
        if( OrdinalComparerForString<T>() is { } ordinal ) Shouldly.ShouldBeEnumerableTestExtensions.ShouldBeInOrder( actual, SortDirection.Ascending, ordinal, customMessage );
        else Shouldly.ShouldBeEnumerableTestExtensions.ShouldBeInOrder( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeInOrder</c>: returns this enumerable. Without comparer, strings are compared with <see cref="StringComparer.Ordinal"/>.</summary>
    public static IEnumerable<T> ShouldBeInOrder<T>( this IEnumerable<T> actual, SortDirection expectedSortDirection, string? customMessage = null )
    {
        if( OrdinalComparerForString<T>() is { } ordinal ) Shouldly.ShouldBeEnumerableTestExtensions.ShouldBeInOrder( actual, expectedSortDirection, ordinal, customMessage );
        else Shouldly.ShouldBeEnumerableTestExtensions.ShouldBeInOrder( actual, expectedSortDirection, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeInOrder</c>: returns this enumerable. Without comparer, strings are compared with <see cref="StringComparer.Ordinal"/>.</summary>
    public static IEnumerable<T> ShouldBeInOrder<T>( this IEnumerable<T> actual, SortDirection expectedSortDirection, IComparer<T>? customComparer, string? customMessage = null )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldBeInOrder( actual, expectedSortDirection, customComparer ?? OrdinalComparerForString<T>(), customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeOfTypes</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldBeOfTypes<T>( this IEnumerable<T> actual, params Type[] expected )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldBeOfTypes( actual, expected );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeOfTypes</c>: returns this enumerable.</summary>
    public static IEnumerable<T> ShouldBeOfTypes<T>( this IEnumerable<T> actual, Type[] expected, string? customMessage )
    {
        Shouldly.ShouldBeEnumerableTestExtensions.ShouldBeOfTypes( actual, expected, customMessage );
        return actual;
    }

    #endregion

    #region Dictionaries: chaining.

    /// <summary>Chaining <c>ShouldContainKey</c>: returns this dictionary.</summary>
    public static IDictionary<TKey, TValue> ShouldContainKey<TKey, TValue>( this IDictionary<TKey, TValue> dictionary, TKey key, string? customMessage = null ) where TKey : notnull
    {
        Shouldly.ShouldBeDictionaryTestExtensions.ShouldContainKey( dictionary, key, customMessage );
        return dictionary;
    }

    /// <summary>Chaining <c>ShouldContainKey</c>: returns this dictionary.</summary>
    [OverloadResolutionPriority( 1 )]
    public static IReadOnlyDictionary<TKey, TValue> ShouldContainKey<TKey, TValue>( this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, string? customMessage = null ) where TKey : notnull
    {
        Shouldly.ShouldBeDictionaryTestExtensions.ShouldContainKey( dictionary, key, customMessage );
        return dictionary;
    }

    /// <summary>Chaining <c>ShouldNotContainKey</c>: returns this dictionary.</summary>
    public static IDictionary<TKey, TValue> ShouldNotContainKey<TKey, TValue>( this IDictionary<TKey, TValue> dictionary, TKey key, string? customMessage = null ) where TKey : notnull
    {
        Shouldly.ShouldBeDictionaryTestExtensions.ShouldNotContainKey( dictionary, key, customMessage );
        return dictionary;
    }

    /// <summary>Chaining <c>ShouldNotContainKey</c>: returns this dictionary.</summary>
    [OverloadResolutionPriority( 1 )]
    public static IReadOnlyDictionary<TKey, TValue> ShouldNotContainKey<TKey, TValue>( this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, string? customMessage = null ) where TKey : notnull
    {
        Shouldly.ShouldBeDictionaryTestExtensions.ShouldNotContainKey( dictionary, key, customMessage );
        return dictionary;
    }

    /// <summary>Chaining <c>ShouldContainKeyAndValue</c>: returns this dictionary.</summary>
    public static IDictionary<TKey, TValue> ShouldContainKeyAndValue<TKey, TValue>( this IDictionary<TKey, TValue> dictionary, TKey key, TValue val, string? customMessage = null ) where TKey : notnull
    {
        Shouldly.ShouldBeDictionaryTestExtensions.ShouldContainKeyAndValue( dictionary, key, val, customMessage );
        return dictionary;
    }

    /// <summary>Chaining <c>ShouldContainKeyAndValue</c>: returns this dictionary.</summary>
    [OverloadResolutionPriority( 1 )]
    public static IReadOnlyDictionary<TKey, TValue> ShouldContainKeyAndValue<TKey, TValue>( this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue val, string? customMessage = null ) where TKey : notnull
    {
        Shouldly.ShouldBeDictionaryTestExtensions.ShouldContainKeyAndValue( dictionary, key, val, customMessage );
        return dictionary;
    }

    /// <summary>Chaining <c>ShouldNotContainValueForKey</c>: returns this dictionary.</summary>
    public static IDictionary<TKey, TValue> ShouldNotContainValueForKey<TKey, TValue>( this IDictionary<TKey, TValue> dictionary, TKey key, TValue val, string? customMessage = null ) where TKey : notnull
    {
        Shouldly.ShouldBeDictionaryTestExtensions.ShouldNotContainValueForKey( dictionary, key, val, customMessage );
        return dictionary;
    }

    /// <summary>Chaining <c>ShouldNotContainValueForKey</c>: returns this dictionary.</summary>
    [OverloadResolutionPriority( 1 )]
    public static IReadOnlyDictionary<TKey, TValue> ShouldNotContainValueForKey<TKey, TValue>( this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue val, string? customMessage = null ) where TKey : notnull
    {
        Shouldly.ShouldBeDictionaryTestExtensions.ShouldNotContainValueForKey( dictionary, key, val, customMessage );
        return dictionary;
    }

    #endregion

    #region Equality: chaining.

    /// <summary>Chaining <c>ShouldBe</c>: returns this value.</summary>
    public static T? ShouldBe<T>( [NotNullIfNotNull( nameof( expected ) )] this T? actual,
                                  [NotNullIfNotNull( nameof( actual ) )] T? expected,
                                  string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c>: returns this value.</summary>
    public static T? ShouldBe<T>( [NotNullIfNotNull( nameof( expected ) )] this T? actual,
                                  [NotNullIfNotNull( nameof( actual ) )] T? expected,
                                  IEqualityComparer<T> comparer,
                                  string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, comparer, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotBe</c>: returns this value.</summary>
    public static T? ShouldNotBe<T>( this T? actual, T? expected, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldNotBe( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotBe</c>: returns this value.</summary>
    public static T? ShouldNotBe<T>( this T? actual, T? expected, IEqualityComparer<T> comparer, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldNotBe( actual, expected, comparer, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c>: returns this enumerable.</summary>
    public static IEnumerable<T>? ShouldBe<T>( [NotNullIfNotNull( nameof( expected ) )] this IEnumerable<T>? actual,
                                               [NotNullIfNotNull( nameof( actual ) )] IEnumerable<T>? expected,
                                               bool ignoreOrder = false )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, ignoreOrder );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c>: returns this enumerable.</summary>
    public static IEnumerable<T>? ShouldBe<T>( [NotNullIfNotNull( nameof( expected ) )] this IEnumerable<T>? actual,
                                               [NotNullIfNotNull( nameof( actual ) )] IEnumerable<T>? expected,
                                               bool ignoreOrder,
                                               string? customMessage )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, ignoreOrder, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c>: returns this enumerable.</summary>
    public static IEnumerable<T>? ShouldBe<T>( [NotNullIfNotNull( nameof( expected ) )] this IEnumerable<T>? actual,
                                               [NotNullIfNotNull( nameof( actual ) )] IEnumerable<T>? expected,
                                               IEqualityComparer<T> comparer,
                                               bool ignoreOrder = false,
                                               string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, comparer, ignoreOrder, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeSameAs</c>: returns this object.</summary>
    public static object? ShouldBeSameAs( [NotNullIfNotNull( nameof( expected ) )] this object? actual,
                                          [NotNullIfNotNull( nameof( actual ) )] object? expected,
                                          string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeSameAs( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotBeSameAs</c>: returns this object.</summary>
    public static object? ShouldNotBeSameAs( this object? actual, object? expected, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldNotBeSameAs( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeOneOf</c>: returns this value.</summary>
    public static T ShouldBeOneOf<T>( this T actual, params T[] expected )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeOneOf( actual, expected );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeOneOf</c>: returns this value.</summary>
    public static T ShouldBeOneOf<T>( this T actual, T[] expected, string? customMessage )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeOneOf( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeOneOf</c>: returns this value.</summary>
    public static T ShouldBeOneOf<T>( this T actual, T[] expected, IEqualityComparer<T> comparer, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeOneOf( actual, expected, comparer, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotBeOneOf</c>: returns this value.</summary>
    public static T ShouldNotBeOneOf<T>( this T actual, params T[] expected )
    {
        Shouldly.ShouldBeTestExtensions.ShouldNotBeOneOf( actual, expected );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotBeOneOf</c>: returns this value.</summary>
    public static T ShouldNotBeOneOf<T>( this T actual, T[] expected, string? customMessage )
    {
        Shouldly.ShouldBeTestExtensions.ShouldNotBeOneOf( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotBeOneOf</c>: returns this value.</summary>
    public static T ShouldNotBeOneOf<T>( this T actual, T[] expected, IEqualityComparer<T> comparer, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldNotBeOneOf( actual, expected, comparer, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldSatisfyAllConditions</c>: returns this value.</summary>
    public static T ShouldSatisfyAllConditions<T>( this T actual, params Action<T>[] conditions )
    {
        Shouldly.ShouldSatisfyAllConditionsTestExtensions.ShouldSatisfyAllConditions( actual, conditions );
        return actual;
    }

    /// <summary>Chaining <c>ShouldSatisfyAllConditions</c>: returns this value.</summary>
    public static T ShouldSatisfyAllConditions<T>( this T actual, string? customMessage, params Action<T>[] conditions )
    {
        Shouldly.ShouldSatisfyAllConditionsTestExtensions.ShouldSatisfyAllConditions( actual, customMessage, conditions );
        return actual;
    }

    /// <summary>Chaining <c>ShouldHaveFlag</c>: returns this value.</summary>
    public static T ShouldHaveFlag<T>( this T actual, T expectedFlag, string? customMessage = null ) where T : struct, Enum
    {
        Shouldly.ShouldlyExtensionMethods.ShouldHaveEnumExtensions.ShouldHaveFlag( actual, expectedFlag, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotHaveFlag</c>: returns this value.</summary>
    public static T ShouldNotHaveFlag<T>( this T actual, T expectedFlag, string? customMessage = null ) where T : struct, Enum
    {
        Shouldly.ShouldlyExtensionMethods.ShouldHaveEnumExtensions.ShouldNotHaveFlag( actual, expectedFlag, customMessage );
        return actual;
    }

    #endregion

    #region Tolerance: chaining.

    /// <summary>Chaining <c>ShouldBe</c> with a tolerance: returns this value.</summary>
    public static double ShouldBe( this double actual, double expected, double tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c> with a tolerance: returns this value.</summary>
    public static float ShouldBe( this float actual, float expected, double tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c> with a tolerance: returns this value.</summary>
    public static decimal ShouldBe( this decimal actual, decimal expected, decimal tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c> with a tolerance: returns this value.</summary>
    public static DateTime ShouldBe( this DateTime actual, DateTime expected, TimeSpan tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c> with a tolerance: returns this value.</summary>
    public static DateTimeOffset ShouldBe( this DateTimeOffset actual, DateTimeOffset expected, TimeSpan tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c> with a tolerance: returns this value.</summary>
    public static TimeSpan ShouldBe( this TimeSpan actual, TimeSpan expected, TimeSpan tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotBe</c> with a tolerance: returns this value.</summary>
    public static DateTime ShouldNotBe( this DateTime actual, DateTime expected, TimeSpan tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldNotBe( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotBe</c> with a tolerance: returns this value.</summary>
    public static DateTimeOffset ShouldNotBe( this DateTimeOffset actual, DateTimeOffset expected, TimeSpan tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldNotBe( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotBe</c> with a tolerance: returns this value.</summary>
    public static TimeSpan ShouldNotBe( this TimeSpan actual, TimeSpan expected, TimeSpan tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldNotBe( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c> with a tolerance: returns this enumerable.</summary>
    public static IEnumerable<decimal> ShouldBe( this IEnumerable<decimal> actual, IEnumerable<decimal> expected, decimal tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c> with a tolerance: returns this enumerable.</summary>
    public static IEnumerable<double> ShouldBe( this IEnumerable<double> actual, IEnumerable<double> expected, double tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, tolerance, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBe</c> with a tolerance: returns this enumerable.</summary>
    public static IEnumerable<float> ShouldBe( this IEnumerable<float> actual, IEnumerable<float> expected, double tolerance, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBe( actual, expected, tolerance, customMessage );
        return actual;
    }

    #endregion

    #region Comparisons: chaining.

    /// <summary>Chaining <c>ShouldBeGreaterThan</c>: returns this value. Without comparer, strings are compared with <see cref="StringComparer.Ordinal"/>.</summary>
    public static T ShouldBeGreaterThan<T>( this T actual, T expected, string? customMessage = null ) where T : IComparable<T>
    {
        if( OrdinalComparerForString<T>() is { } ordinal ) Shouldly.ShouldBeTestExtensions.ShouldBeGreaterThan( actual, expected, ordinal, customMessage );
        else Shouldly.ShouldBeTestExtensions.ShouldBeGreaterThan( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeGreaterThan</c>: returns this value.</summary>
    public static T ShouldBeGreaterThan<T>( this T actual, T expected, IComparer<T> comparer, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeGreaterThan( actual, expected, comparer, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeGreaterThanOrEqualTo</c>: returns this value. Without comparer, strings are compared with <see cref="StringComparer.Ordinal"/>.</summary>
    public static T ShouldBeGreaterThanOrEqualTo<T>( this T actual, T expected, string? customMessage = null ) where T : IComparable<T>
    {
        if( OrdinalComparerForString<T>() is { } ordinal ) Shouldly.ShouldBeTestExtensions.ShouldBeGreaterThanOrEqualTo( actual, expected, ordinal, customMessage );
        else Shouldly.ShouldBeTestExtensions.ShouldBeGreaterThanOrEqualTo( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeGreaterThanOrEqualTo</c>: returns this value.</summary>
    public static T ShouldBeGreaterThanOrEqualTo<T>( this T actual, T expected, IComparer<T> comparer, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeGreaterThanOrEqualTo( actual, expected, comparer, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeLessThan</c>: returns this value. Without comparer, strings are compared with <see cref="StringComparer.Ordinal"/>.</summary>
    public static T ShouldBeLessThan<T>( this T actual, T expected, string? customMessage = null ) where T : IComparable<T>
    {
        if( OrdinalComparerForString<T>() is { } ordinal ) Shouldly.ShouldBeTestExtensions.ShouldBeLessThan( actual, expected, ordinal, customMessage );
        else Shouldly.ShouldBeTestExtensions.ShouldBeLessThan( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeLessThan</c>: returns this value.</summary>
    public static T ShouldBeLessThan<T>( this T actual, T expected, IComparer<T> comparer, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeLessThan( actual, expected, comparer, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeLessThanOrEqualTo</c>: returns this value. Without comparer, strings are compared with <see cref="StringComparer.Ordinal"/>.</summary>
    public static T ShouldBeLessThanOrEqualTo<T>( this T actual, T expected, string? customMessage = null ) where T : IComparable<T>
    {
        if( OrdinalComparerForString<T>() is { } ordinal ) Shouldly.ShouldBeTestExtensions.ShouldBeLessThanOrEqualTo( actual, expected, ordinal, customMessage );
        else Shouldly.ShouldBeTestExtensions.ShouldBeLessThanOrEqualTo( actual, expected, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeLessThanOrEqualTo</c>: returns this value.</summary>
    public static T ShouldBeLessThanOrEqualTo<T>( this T actual, T expected, IComparer<T> comparer, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeLessThanOrEqualTo( actual, expected, comparer, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeInRange</c>: returns this value. Strings are compared with <see cref="StringComparer.Ordinal"/>.</summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static T ShouldBeInRange<T>( this T actual, T from, T to, string? customMessage = null ) where T : IComparable<T>
    {
        if( OrdinalComparerForString<T>() is { } ordinal ) actual.AssertAwesomely( v => ordinal.Compare( v, from ) >= 0 && ordinal.Compare( v, to ) <= 0, actual, new { from, to }, customMessage );
        else Shouldly.ShouldBeTestExtensions.ShouldBeInRange( actual, from, to, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldNotBeInRange</c>: returns this value. Strings are compared with <see cref="StringComparer.Ordinal"/>.</summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static T ShouldNotBeInRange<T>( this T actual, T from, T to, string? customMessage = null ) where T : IComparable<T>
    {
        if( OrdinalComparerForString<T>() is { } ordinal ) actual.AssertAwesomely( v => ordinal.Compare( v, from ) < 0 || ordinal.Compare( v, to ) > 0, actual, new { from, to }, customMessage );
        else Shouldly.ShouldBeTestExtensions.ShouldNotBeInRange( actual, from, to, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBePositive</c>: returns this value.</summary>
    public static decimal ShouldBePositive( this decimal actual, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBePositive( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBePositive</c>: returns this value.</summary>
    public static double ShouldBePositive( this double actual, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBePositive( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBePositive</c>: returns this value.</summary>
    public static float ShouldBePositive( this float actual, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBePositive( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBePositive</c>: returns this value.</summary>
    public static int ShouldBePositive( this int actual, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBePositive( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBePositive</c>: returns this value.</summary>
    public static long ShouldBePositive( this long actual, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBePositive( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBePositive</c>: returns this value.</summary>
    public static short ShouldBePositive( this short actual, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBePositive( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeNegative</c>: returns this value.</summary>
    public static decimal ShouldBeNegative( this decimal actual, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeNegative( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeNegative</c>: returns this value.</summary>
    public static double ShouldBeNegative( this double actual, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeNegative( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeNegative</c>: returns this value.</summary>
    public static float ShouldBeNegative( this float actual, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeNegative( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeNegative</c>: returns this value.</summary>
    public static int ShouldBeNegative( this int actual, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeNegative( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeNegative</c>: returns this value.</summary>
    public static long ShouldBeNegative( this long actual, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeNegative( actual, customMessage );
        return actual;
    }

    /// <summary>Chaining <c>ShouldBeNegative</c>: returns this value.</summary>
    public static short ShouldBeNegative( this short actual, string? customMessage = null )
    {
        Shouldly.ShouldBeTestExtensions.ShouldBeNegative( actual, customMessage );
        return actual;
    }

    #endregion

    // Shouldly orders with Comparer<T>.Default: for strings, this uses the current culture.
    static IComparer<T>? OrdinalComparerForString<T>() => typeof( T ) == typeof( string ) ? (IComparer<T>)(object)StringComparer.Ordinal : null;
}

#pragma warning restore CA1050 // Declare types in namespaces

namespace Shouldly
{

    /// <summary>
    /// Extends Shouldly with useful helpers.
    /// </summary>
    [DebuggerStepThrough]
    [ShouldlyMethods]
    [EditorBrowsable( EditorBrowsableState.Never )]
    public static class CKShouldlyExtensions
    {
        /// <summary>
        /// Fix https://github.com/shouldly/shouldly/issues/934: Shouldly's ShouldThrow accepts only
        /// an <see cref="Action"/>, a <c>Func&lt;object?&gt;</c> or a <c>Func&lt;Task&gt;</c>.
        /// <para>
        /// This accepts any delegate without parameters (a <c>Func&lt;int&gt;</c> for instance).
        /// When the delegate returns a <see cref="Task"/> or a <see cref="ValueTask"/>, it is awaited.
        /// </para>
        /// </summary>
        /// <typeparam name="TException">The expected exception type.</typeparam>
        /// <param name="actual">The action code that should throw.</param>
        /// <param name="customMessage">Optional message.</param>
        /// <returns>The exception instance.</returns>
        public static TException ShouldThrow<TException>( this Delegate actual, string? customMessage = null )
            where TException : Exception
        {
            return (TException)ThrowInternal( actual, customMessage, typeof( TException ), exactType: false );
        }

        /// <summary>
        /// Same as <see cref="ShouldThrow{TException}(Delegate, string?)"/> but the exception must be
        /// exactly a <typeparamref name="TException"/>, not a specialization.
        /// </summary>
        /// <typeparam name="TException">The expected exception type.</typeparam>
        /// <param name="actual">The action code that should throw.</param>
        /// <param name="customMessage">Optional message.</param>
        /// <returns>The exception instance.</returns>
        public static TException ShouldThrowExactly<TException>( this Delegate actual, string? customMessage = null )
            where TException : Exception
        {
            return (TException)ThrowInternal( actual, customMessage, typeof( TException ), exactType: true );
        }

        static Exception ThrowInternal( Delegate actual,
                                        string? customMessage,
                                        Type expectedExceptionType,
                                        bool exactType,
                                        [CallerMemberName] string? shouldlyMethod = null )
        {
            Throw.CheckNotNullArgument( actual );
            // Invoking the delegate's Invoke method handles a multicast delegate and a delegate
            // bound to its first parameter (a method group of an extension method).
            var invoke = actual.GetType().GetMethod( "Invoke" )!;
            if( invoke.GetParameters().Length > 0 )
            {
                throw new ArgumentException( $"ShouldThrow can only be called on a delegate without parameters. Delegate type is '{actual.GetType().ToCSharpName( withNamespace: false )}'." );
            }
            try
            {
                var result = invoke.Invoke( actual, BindingFlags.DoNotWrapExceptions, null, null, null );
                if( result is Task task )
                {
                    task.GetAwaiter().GetResult();
                }
                else if( result is ValueTask valueTask )
                {
                    valueTask.GetAwaiter().GetResult();
                }
                else if( result != null
                         && result.GetType().IsGenericType
                         && result.GetType().GetGenericTypeDefinition() == typeof( ValueTask<> ) )
                {
                    ((Task)result.GetType().GetMethod( nameof( ValueTask<int>.AsTask ) )!.Invoke( result, null )!).GetAwaiter().GetResult();
                }
            }
            catch( Exception ex )
            {
                if( ex.GetType() == expectedExceptionType
                    || (!exactType && expectedExceptionType.IsAssignableFrom( ex.GetType() )) )
                {
                    return ex;
                }
                throw new ShouldAssertException( new ShouldlyThrowMessage( expectedExceptionType, ex.GetType(), customMessage, shouldlyMethod! ).ToString(), ex );
            }
            throw new ShouldAssertException( new ShouldlyThrowMessage( expectedExceptionType, customMessage: customMessage, shouldlyMethod! ).ToString() );
        }

        /// <summary>
        /// Predicate match.
        /// This is CK specific.
        /// </summary>
        /// <typeparam name="T">This type.</typeparam>
        /// <param name="actual">This instance.</param>
        /// <param name="predicate">The predicate that muts be satisfied.</param>
        /// <param name="customMessage">Optional message.</param>
        /// <returns>This instance.</returns>
        [MethodImpl( MethodImplOptions.NoInlining )]
        public static T ShouldMatch<T>( this T actual, Expression<Func<T, bool>> predicate, string? customMessage = null )
        {
            Throw.CheckNotNullArgument( predicate );
            var condition = predicate.Compile();
            if( !condition( actual ) )
                throw new ShouldAssertException( new ExpectedActualShouldlyMessage( predicate.Body, actual, customMessage ).ToString() );
            return actual;
        }

        /// <summary>
        /// Apply an action to each item that should be one or more Shouldly expectation.
        /// This is CK specific.
        /// </summary>
        /// <typeparam name="T">Th type of the enumerable.</typeparam>
        /// <param name="actual">This enumerable.</param>
        /// <param name="action">The action to apply.</param>
        /// <returns>This enumerable.</returns>
        public static IEnumerable<T> ShouldAll<T>( this IEnumerable<T> actual, Action<T> action )
        {
            Throw.CheckNotNullArgument( action );
            int idx = 0;
            try
            {
                foreach( var e in actual )
                {
                    action( e );
                    ++idx;
                }
            }
            catch( ShouldAssertException aEx )
            {
                var prefix = Environment.NewLine + "  | ";
                var offsetMessage = string.Join( prefix, aEx.Message.Split( Environment.NewLine ) );
                throw new ShouldAssertException( $"ShouldAll failed for item n°{idx}.{prefix}{offsetMessage}" );
            }
            return actual;
        }

        /// <summary>
        /// Explicit override to allow implict cast from string.
        /// </summary>
        /// <param name="actual">This normalized path.</param>
        /// <param name="expected">The expected path.</param>
        /// <param name="customMessage">Optional message.</param>
        /// <returns>This path.</returns>
        [MethodImpl( MethodImplOptions.NoInlining )]
        public static NormalizedPath ShouldBe( this NormalizedPath actual, NormalizedPath expected, string? customMessage = null )
        {
            actual.AssertAwesomely( actual => actual == expected, actual, expected, customMessage );
            return actual;
        }

        /// <summary>
        /// Explicit overload to allow implicit conversion from integer.
        /// </summary>
        /// <param name="actual">This value.</param>
        /// <param name="expected">Expected value.</param>
        /// <param name="customMessage">Optional message.</param>
        /// <returns>This value.</returns>
        [MethodImpl( MethodImplOptions.NoInlining )]
        public static ulong ShouldBe( this ulong actual, ulong expected, string? customMessage = null )
        {
            actual.AssertAwesomely( actual => actual == expected, actual, expected, customMessage );
            return actual;
        }

        /// <inheritdoc cref="ShouldBe(ulong, ulong, string?)"/>
        [MethodImpl( MethodImplOptions.NoInlining )]
        public static uint ShouldBe( this uint actual, uint expected, string? customMessage = null )
        {
            actual.AssertAwesomely( actual => actual == expected, actual, expected, customMessage );
            return actual;
        }

        /// <inheritdoc cref="ShouldBe(ulong, ulong, string?)"/>
        [MethodImpl( MethodImplOptions.NoInlining )]
        public static short ShouldBe( this short actual, short expected, string? customMessage = null )
        {
            actual.AssertAwesomely( actual => actual == expected, actual, expected, customMessage );
            return actual;
        }

        /// <inheritdoc cref="ShouldBe(ulong, ulong, string?)"/>
        [MethodImpl( MethodImplOptions.NoInlining )]
        public static ushort ShouldBe( this ushort actual, ushort expected, string? customMessage = null )
        {
            actual.AssertAwesomely( actual => actual == expected, actual, expected, customMessage );
            return actual;
        }

        /// <inheritdoc cref="ShouldBe(ulong, ulong, string?)"/>
        [MethodImpl( MethodImplOptions.NoInlining )]
        public static sbyte ShouldBe( this sbyte actual, sbyte expected, string? customMessage = null )
        {
            actual.AssertAwesomely( actual => actual == expected, actual, expected, customMessage );
            return actual;
        }

        /// <inheritdoc cref="ShouldBe(ulong, ulong, string?)"/>
        [MethodImpl( MethodImplOptions.NoInlining )]
        public static byte ShouldBe( this byte actual, byte expected, string? customMessage = null )
        {
            actual.AssertAwesomely( actual => actual == expected, actual, expected, customMessage );
            return actual;
        }

    }
}
