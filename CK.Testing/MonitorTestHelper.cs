using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;
using CK.Core.Json;
using CK.Monitoring;
using CK.Monitoring.Handlers;
using Microsoft.IO;

namespace CK.Testing;


/// <summary>
/// Provides the <see cref="TestHelper"/>: the single implementation of <see cref="IMonitorTestHelper"/>.
/// <para>
/// Use <c>using static CK.Testing.MonitorTestHelper;</c> to get it.
/// </para>
/// </summary>
public sealed class MonitorTestHelper : IMonitorTestHelper
{
    const int _maxCurrentLogFolderCount = 5;
    const int _maxArchivedLogFolderCount = 20;

    static readonly object _lock = new object();
    static MonitorTestHelper? _testHelper;

    readonly NormalizedPath _closestSUTProjectFolder;
    readonly IActivityMonitor _monitor;
    readonly ActivityMonitorConsoleClient _console;
    readonly bool _logToCKMon;
    readonly bool _logToText;

    MonitorTestHelper()
    {
        _closestSUTProjectFolder = StaticTestHelper.GetClosestSUTProjectCandidatePaths( StaticTestHelper._solutionFolder, StaticTestHelper._testProjectFolder )
                                                   .FirstOrDefault( p => Directory.Exists( p ) );
        if( _closestSUTProjectFolder.IsEmptyPath )
        {
            _closestSUTProjectFolder = StaticTestHelper._testProjectFolder;
        }

        _logToCKMon = GetBooleanSetting( "Monitor/LogToCKMon", true );
        _logToText = GetBooleanSetting( "Monitor/LogToText", true );
        bool logToConsole = GetBooleanSetting( "Monitor/LogToConsole", false );
        // LogLevel defaults to Debug while testing.
        var logLevel = StaticTestHelper.GetSetting( "Monitor/LogLevel" );
        ActivityMonitor.DefaultFilter = logLevel == null ? LogFilter.Debug : LogFilter.Parse( logLevel );

        LogFile.RootLogPath = StaticTestHelper._logFolder;
        var conf = new GrandOutputConfiguration();
        if( _logToCKMon )
        {
            var binConf = new BinaryFileConfiguration
            {
                UseGzipCompression = true,
                Path = "CKMon",
                TimedFolderMode =
                {
                    MaxCurrentLogFolderCount = _maxCurrentLogFolderCount,
                    MaxArchivedLogFolderCount = _maxArchivedLogFolderCount,
                }
            };
            conf.AddHandler( binConf );
        }
        if( _logToText )
        {
            var txtConf = new TextFileConfiguration
            {
                Path = "Text",
                TimedFolderMode =
                {
                    MaxCurrentLogFolderCount = _maxCurrentLogFolderCount,
                    MaxArchivedLogFolderCount = _maxArchivedLogFolderCount,
                }
            };
            conf.AddHandler( txtConf );
        }
        GrandOutput.EnsureActiveDefault( conf, clearExistingTraceListeners: false );
        var monitorListener = Trace.Listeners.OfType<MonitorTraceListener>().FirstOrDefault( m => m.GrandOutput == GrandOutput.Default );
        // If our standard MonitorTraceListener has been injected by the GrandOuput, then we remove the StaticTestHelper.SafeTraceListener
        // that always throws Exceptions and never calls FailFast.
        // (Defensive programming) There is no real reason for this listener to not be in the listeners, but it can be.
        if( monitorListener != null )
        {
            Trace.Listeners.Remove( "CK.Testing.SafeTraceListener" );
        }
        _monitor = new ActivityMonitor( "MonitorTestHelper" );
        _console = new ActivityMonitorConsoleClient();
        LogToConsole = logToConsole;
    }

    static bool GetBooleanSetting( string key, bool defaultValue )
    {
        var v = StaticTestHelper.GetSetting( key );
        if( v == null ) return defaultValue;
        if( !bool.TryParse( v, out var b ) )
        {
            Throw.InvalidOperationException( $"Setting '{key}' must be 'true' or 'false'. Value: '{v}'." );
        }
        return b;
    }

    /// <summary>
    /// Gets the <see cref="IMonitorTestHelper"/>.
    /// <para>
    /// It is created on the first access. This activates the <see cref="GrandOutput.Default"/> that writes the log files.
    /// </para>
    /// </summary>
    public static IMonitorTestHelper TestHelper => Volatile.Read( ref _testHelper ) ?? Create();

    static MonitorTestHelper Create()
    {
        lock( _lock )
        {
            if( _testHelper == null )
            {
                StaticTestHelper.EnsureInitialized();
                Volatile.Write( ref _testHelper, new MonitorTestHelper() );
            }
            return _testHelper;
        }
    }

    string? IMonitorTestHelper.GetSetting( string key ) => StaticTestHelper.GetSetting( key );

    string IMonitorTestHelper.BuildConfiguration => StaticTestHelper._buildConfiguration;

    string IMonitorTestHelper.TestProjectName => StaticTestHelper._testProjectFolder.LastPart;

    string IMonitorTestHelper.SolutionName => StaticTestHelper._solutionName;

    NormalizedPath IMonitorTestHelper.SolutionFolder => StaticTestHelper._solutionFolder;

    NormalizedPath IMonitorTestHelper.TestProjectFolder => StaticTestHelper._testProjectFolder;

    NormalizedPath IMonitorTestHelper.ClosestSUTProjectFolder => _closestSUTProjectFolder;

    NormalizedPath IMonitorTestHelper.LogFolder => StaticTestHelper._logFolder;

    NormalizedPath IMonitorTestHelper.BinFolder => StaticTestHelper._binFolder;

    NormalizedPath IMonitorTestHelper.PathToBin => StaticTestHelper._pathToBin;

    IActivityMonitor IMonitorTestHelper.Monitor
    {
        [DebuggerStepThrough]
        get => _monitor;
    }

    bool LogToConsole
    {
        get => _monitor.Output.Clients.Contains( _console );
        set
        {
            if( _monitor.Output.Clients.Contains( _console ) != value )
            {
                if( value )
                {
                    _monitor.Output.RegisterClient( _console );
                    _monitor.Info( "Switching console log ON." );
                }
                else
                {
                    _monitor.Info( "Switching console log OFF." );
                    _monitor.Output.UnregisterClient( _console );
                }
            }
        }
    }

    bool IMonitorTestHelper.LogToConsole
    {
        get => LogToConsole;
        set => LogToConsole = value;
    }

    bool IMonitorTestHelper.LogToCKMon => _logToCKMon;

    bool IMonitorTestHelper.LogToText => _logToText;

    IDisposable IMonitorTestHelper.TemporaryEnsureConsoleMonitor()
    {
        bool prev = LogToConsole;
        LogToConsole = true;
        return Util.CreateDisposableAction( () => LogToConsole = prev );
    }

    sealed class Resumer
    {
        internal readonly TaskCompletionSource _tcs;
        readonly Timer _timer;
        readonly Func<bool, bool> _resume;
        bool _reentrant;

        internal Resumer( Func<bool, bool> resumeF )
        {
            _timer = new Timer( OnTimer, null, 1000, 1000 );
            _tcs = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
            _resume = resumeF;
        }

        void OnTimer( object? _ )
        {
            if( _reentrant ) return;
            _reentrant = true;
            if( _resume( false ) )
            {
                _tcs.SetResult();
                _timer.Dispose();
            }
            _reentrant = false;
        }
    }

    Task IMonitorTestHelper.SuspendAsync( Func<bool, bool> resume,
                                          string? testName,
                                          int lineNumber,
                                          string? fileName )
    {
        Throw.CheckNotNullArgument( resume );
        if( !Debugger.IsAttached )
        {
            _monitor.Warn( $"TestHelper.SuspendAsync called from '{testName}' method while no debugger is attached. Ignoring it.", lineNumber, fileName );
            return Task.CompletedTask;
        }
        _monitor.Info( $"TestHelper.SuspendAsync called from '{testName}' method.", lineNumber, fileName );
        return new Resumer( resume )._tcs.Task;
    }

    NormalizedPath IMonitorTestHelper.CleanupFolder( NormalizedPath folder, bool ensureFolderAvailable, int maxRetryCount )
    {
        Throw.CheckArgument( !folder.IsEmptyPath );
        int tryCount = 0;
        for(; ; )
        {
            try
            {
                if( Directory.Exists( folder ) ) Directory.Delete( folder, true );
                if( ensureFolderAvailable )
                {
                    Directory.CreateDirectory( folder );
                    File.WriteAllText( Path.Combine( folder, "TestWrite.txt" ), "Test write works." );
                    File.Delete( Path.Combine( folder, "TestWrite.txt" ) );
                }
                _monitor.Info( $"Folder '{folder}' has been cleaned up." );
                return folder;
            }
            catch( Exception ex )
            {
                if( ++tryCount > maxRetryCount )
                {
                    throw new CKException( $"Unable to cleanup folder '{folder}'.", ex );
                }
                Thread.Sleep( 100 );
            }
        }
    }

    void IMonitorTestHelper.OnlyOnce( Action a, string? s, int l )
    {
        var key = s + l.ToString();
        bool shouldRun;
        lock( StaticTestHelper._onlyOnce )
        {
            shouldRun = StaticTestHelper._onlyOnce.Add( key );
        }
        if( shouldRun ) a();
    }

    T IMonitorTestHelper.JsonIdempotenceCheck<T>( T o,
                                                  Action<Utf8JsonWriter, T> write,
                                                  Utf8JsonReaderDelegate<T> read,
                                                  IUtf8JsonReaderContext? readerContext,
                                                  Action<string>? jsonText1,
                                                  Action<string>? jsonText2 )
    {
        // This is safe: a Utf8JsonReaderDelegate<T> is a Utf8JsonReaderDelegate<T,IUtf8JsonReaderContext>.
        return JsonIdempotenceCheck( o,
                                     write,
                                     Unsafe.As<Utf8JsonReaderDelegate<T, IUtf8JsonReaderContext>>( read ),
                                     readerContext ?? IUtf8JsonReaderContext.Empty,
                                     jsonText1,
                                     jsonText2 );
    }

    T IMonitorTestHelper.JsonIdempotenceCheck<T, TReadContext>( T o,
                                                                Action<Utf8JsonWriter, T> write,
                                                                Utf8JsonReaderDelegate<T, TReadContext> read,
                                                                TReadContext readerContext,
                                                                Action<string>? jsonText1,
                                                                Action<string>? jsonText2 )
    {
        return JsonIdempotenceCheck( o, write, read, readerContext, jsonText1, jsonText2 );
    }

    static T JsonIdempotenceCheck<T, TReadContext>( T o,
                                                    Action<Utf8JsonWriter, T> write,
                                                    Utf8JsonReaderDelegate<T, TReadContext> read,
                                                    TReadContext readerContext,
                                                    Action<string>? jsonText1,
                                                    Action<string>? jsonText2 )
        where TReadContext : class, IUtf8JsonReaderContext
    {
        using( var m = (RecyclableMemoryStream)Util.RecyclableStreamManager.GetStream() )
        using( Utf8JsonWriter w = new Utf8JsonWriter( (IBufferWriter<byte>)m ) )
        {
            write( w, o );
            w.Flush();
            string? text1 = Encoding.UTF8.GetString( m.GetReadOnlySequence() );
            jsonText1?.Invoke( text1 );
            var reader = new Utf8JsonReader( m.GetReadOnlySequence() );
            Throw.DebugAssert( reader.TokenType == JsonTokenType.None );
            reader.ReadWithMoreData( readerContext );
            var oBack = read( ref reader, readerContext );
            if( oBack == null )
            {
                Throw.CKException( $"A null has been read back from '{text1}' for a non null instance of '{typeof( T ).ToCSharpName()}'." );
            }
            string? text2 = null;
            m.Position = 0;
            using( var w2 = new Utf8JsonWriter( (IBufferWriter<byte>)m ) )
            {
                write( w2, oBack );
                w2.Flush();
                // GetReadOnlySequence() will get the end of the previously written buffer.
                // Slice is required.
                text2 = Encoding.UTF8.GetString( m.GetReadOnlySequence().Slice( 0, m.Position ) );
            }
            jsonText2?.Invoke( text2 );
            if( text1 != text2 )
            {
                Throw.CKException( $"""
                            Json idempotence failure between first write:
                            {text1}

                            And second write of the read back {typeof( T ).ToCSharpName()} instance:
                            {text2}

                            """ );
            }
            return oBack;
        }
    }
}
