using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;
using CK.Monitoring;
using CK.Monitoring.Handlers;
using CK.Testing.Monitoring;

namespace CK.Testing;


/// <summary>
/// Provides default implementation of <see cref="IMonitorTestHelperCore"/>
/// and easy to use accessor to the <see cref="IMonitorTestHelper"/> mixin.
/// </summary>
public sealed class MonitorTestHelper : IMonitorTestHelperCore
{
    const int _maxCurrentLogFolderCount = 5;
    const int _maxArchivedLogFolderCount = 20;

    readonly IActivityMonitor _monitor;
    readonly ActivityMonitorConsoleClient _console;
    readonly TestHelperConfiguration _config;
    readonly IBasicTestHelper _basic;
    static bool _logToCKMon;
    static bool _logToText;

    internal MonitorTestHelper( TestHelperConfiguration config, IBasicTestHelper basic )
    {
        _config = config;
        _basic = basic;

        // Defensive programming: even if more than one MonitorTestHelper is instantiated, the GrandOutput and the related
        // configurations must be initialized once.
        basic.OnlyOnce( () =>
        {
            _logToCKMon = _config.DeclareBoolean( "Monitor/LogToCKMon",
                                                  true,
                                                  $"Emits binary logs to {_basic.LogFolder}/CKMon folder.",
                                                  null,
                                                  "Monitor/LogToBinFile",
                                                  "Monitor/LogToBinFiles" ).Value;

            _logToText = _config.DeclareBoolean( "Monitor/LogToText",
                                                  true,
                                                  $"Emits text logs to {_basic.LogFolder}/Text folder.",
                                                  null,
                                                  "Monitor/LogToTextFile",
                                                  "Monitor/LogToTextFiles" ).Value;

            // LogLevel defaults to Debug while testing.
            string logLevel = _config.Declare( "Monitor/LogLevel",
                                               "Debug",
                                               "Initializes the static ActivityMonitor.DefaultFilter value.",
                                               () => ActivityMonitor.DefaultFilter.ToString() ).Value;
            if( logLevel == null )
            {
                ActivityMonitor.DefaultFilter = LogFilter.Debug;
            }
            else
            {
                var lf = LogFilter.Parse( logLevel );
                ActivityMonitor.DefaultFilter = lf;
            }
            LogFile.RootLogPath = basic.LogFolder;
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
            // If our standard MonitorTraceListener has been injected by the GrandOuput, then we remove the StaticBasicTestHelper.SafeTraceListener
            // that always throws Exceptions and never calls FailFast.
            // (Defensive programming) There is no real reason for this listener to not be in the listeners, but it can be.
            if( monitorListener != null )
            {
                Trace.Listeners.Remove( "CK.Testing.SafeTraceListener" );
            }
        } );
        _monitor = new ActivityMonitor( "MonitorTestHelper" );
        _console = new ActivityMonitorConsoleClient();
        LogToConsole = _config.DeclareBoolean( "Monitor/LogToConsole",
                                               false,
                                               "Writes the text logs to the console.",
                                               () => LogToConsole.ToString() ).Value;
        basic.OnCleanupFolder += OnCleanupFolder;
    }

    void OnCleanupFolder( object? sender, CleanupFolderEventArgs e )
    {
        _monitor.Info( $"Folder '{e.Folder}' has been cleaned up." );
    }

    IActivityMonitor IMonitorTestHelperCore.Monitor
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

    bool IMonitorTestHelperCore.LogToConsole
    {
        get => LogToConsole;
        set => LogToConsole = value;
    }

    bool IMonitorTestHelperCore.LogToCKMon => _logToCKMon;

    bool IMonitorTestHelperCore.LogToText => _logToText;

    IDisposable IMonitorTestHelperCore.TemporaryEnsureConsoleMonitor()
    {
        bool prev = LogToConsole;
        LogToConsole = true;
        return Util.CreateDisposableAction( () => LogToConsole = prev );
    }

    void ITestHelperResolvedCallback.OnTestHelperGraphResolved( object finalMixin )
    {
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

    Task IMonitorTestHelperCore.SuspendAsync( Func<bool, bool> resume,
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

    /// <summary>
    /// Gets the <see cref="IMonitorTestHelper"/> mixin.
    /// </summary>
    public static IMonitorTestHelper TestHelper => TestHelperResolver.Default.Resolve<IMonitorTestHelper>();

}
