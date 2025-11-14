using System;
using NUnit.Framework;
using CK.Core;
using CK.Monitoring;
using System.IO;
using System.Linq;
using System.Text;

using static CK.Testing.MonitorTestHelper;
using System.Threading.Tasks;
using Shouldly;
using System.Net.Mime;

namespace GlobalLogs.Tests;

[TestFixture]
public class GlobalLoggingTests
{
    [Test]
    public async Task ckmon_files_and_text_files_are_produced_Async()
    {
        var secret = Guid.NewGuid().ToString();
        var binSecret = Encoding.UTF8.GetBytes( secret );

        TestHelper.Monitor.Info( $"This will appear in ckmon, text files and the console: {secret}" );
        Throw.DebugAssert( GrandOutput.Default != null );
        await GrandOutput.Default.DisposeAsync();

        var logFiles = Directory.EnumerateFiles( TestHelper.LogFolder, "*.log", SearchOption.AllDirectories )
                                .Select( f => (FileName: Path.GetFileName( f ), Content: File.ReadAllText( f )) )
                                .Where( f => f.Content.Contains( secret ) )
                                .ToArray();
        // If the user can create SymLinks, the LastRun.log is here.
        logFiles.Length.ShouldMatch( c => c == 1 || c == 2 );
        if( logFiles.Length == 2 )
        {
            logFiles.Any( f => f.FileName == "LastRun.log" ).ShouldBeTrue();
        }

        // ckmon files are now gzipped by default.
        int count = 0;
        foreach( var fName in Directory.EnumerateFiles( TestHelper.LogFolder, "*.ckmon", SearchOption.AllDirectories ) )
        {
            using( var input = LogReader.Open( fName ) )
            {
                while( input.MoveNext() )
                {
                    if( input.Current.LogType != LogEntryType.CloseGroup
                        && input.Current.Text != null && input.Current.Text.Contains( secret ) )
                    {
                        ++count;
                    }
                }
            }
        }
        count.ShouldBe( 1 );
    }

    [Test]
    public void TestHelper_properties_are_available()
    {
        var w = new StringWriter();
        DumpProperties( w, "> ", TestHelper );
        var text = w.ToString();
        text.ShouldContain( "LogToCKMon = True" );
        TestHelper.Monitor.Info( text );
    }

    static void DumpProperties( TextWriter w, string prefix, object o )
    {
        foreach( var p in o.GetType().GetProperties() )
        {
            if( p.PropertyType.IsValueType || p.PropertyType == typeof( string ) )
            {
                w.WriteLine( $"{prefix}{p.Name} = {p.GetValue( o ) ?? "<null>"}" );
            }
            else if( typeof( System.Collections.IEnumerable ).IsAssignableFrom( p.PropertyType ) )
            {
                w.Write( $"{prefix}{p.Name} = " );
                if( p.GetValue( o ) is not System.Collections.IEnumerable items ) w.WriteLine( "<null>" );
                else
                {
                    var wPrefix = new String( ' ', prefix.Length + p.Name.Length + 3 );
                    w.WriteLine( $"[" );
                    foreach( var item in items )
                    {
                        Type t = item.GetType();
                        if( t.IsValueType || t == typeof( string ) )
                        {
                            w.Write( wPrefix );
                            w.WriteLine( item );
                        }
                    }
                    w.WriteLine( $"{wPrefix}]" );
                }
            }
        }
    }

}
