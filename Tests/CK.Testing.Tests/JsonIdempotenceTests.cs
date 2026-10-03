using CK.Core;
using Shouldly;
using NUnit.Framework;
using System.Text.Json;

namespace CK.Testing.Tests;

[TestFixture]
public class JsonIdempotenceTests
{
    [Test]
    public void rewriting_shorter_does_not_show_the_trailing()
    {
        var h = MonitorTestHelper.TestHelper;
        h.JsonIdempotenceCheck( "long initial write.", Writer, Reader ).ShouldBe( "long initial write." );

        string? text1 = null, text2 = null;
        Util.Invokable( () => h.JsonIdempotenceCheck( "long initial write.", Writer, BuggyReader, jsonText1: t => text1 = t, jsonText2: t => text2 = t ) )
            .ShouldThrow<CKException>()
            .Message.ShouldBe( """
                Json idempotence failure between first write:
                {"P":"long initial write."}

                And second write of the read back string instance:
                {"P":"long in"}

                """ );
        text1.ShouldBe( """{"P":"long initial write."}""" );
        text2.ShouldBe( """{"P":"long in"}""" );
    }

    static void Writer( Utf8JsonWriter writer, string s )
    {
        writer.WriteStartObject();
        writer.WriteString( "P", s );
        writer.WriteEndObject();
    }

    static string Reader( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        r.Read();
        r.GetString().ShouldBe( "P" );
        r.Read();
        var s = r.GetString();
        r.Read();
        r.Read();
        return s!;
    }

    static string BuggyReader( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        return Reader( ref r, context ).Substring( 0, 7 );
    }
}
