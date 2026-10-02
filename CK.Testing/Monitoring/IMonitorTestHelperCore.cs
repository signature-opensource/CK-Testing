using CK.Core;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Testing.Monitoring;

/// <summary>
/// Provides a monitor and console control.
/// </summary>
public interface IMonitorTestHelperCore : ITestHelperResolvedCallback
{
    /// <summary>
    /// Gets the monitor.
    /// </summary>
    IActivityMonitor Monitor { get; }

    /// <summary>
    /// Gets or sets whether <see cref="Monitor"/> will log into the console.
    /// Initially configurable by "Monitor/LogToConsole" = "true", otherwise defaults to false.
    /// </summary>
    bool LogToConsole { get; set; }

    /// <summary>
    /// Gets whether all activities will be logged to <see cref="IBasicTestHelper.LogFolder"/>/CKMon folders.
    /// Configurable by "Monitor/LogToCKMon" = "false".
    /// <para>
    /// This defaults to true.
    /// </para>
    /// </summary>
    bool LogToCKMon { get; }

    /// <summary>
    /// Gets whether all activities will be logged to <see cref="IBasicTestHelper.LogFolder"/>/Text folders.
    /// Configurable by "Monitor/LogToText" = "false".
    /// <para>
    /// This defaults to true.
    /// </para>
    /// </summary>
    bool LogToText { get; }

    /// <summary>
    /// Ensures that the console monitor is on (i.e. <see cref="LogToConsole"/> is true) until the
    /// returned IDisposable is disposed.
    /// </summary>
    /// <returns>The disposable.</returns>
    IDisposable TemporaryEnsureConsoleMonitor();

    /// <summary>
    /// Asynchronously blocks until true is returned from the callback (the callback is called every second).
    /// This can be used only when <see cref="Debugger.IsAttached"/> is true: this is ignored otherwise. 
    /// <para>
    /// This is intended to let context alive for an undetermined delay, this can be seen as an interruptible
    /// <c>await Task.Delay( Timeout.Infinite );</c> or a breakpoint that suspends the current task but let
    /// all the other tasks and threads run.
    /// </para>
    /// <para>
    /// Usage: set a breakpoint in the callback and set the resume variable to true (typically via the watch window)
    /// to continue the execution.
    /// <code>
    ///                                  Put a breakpoint here                                         
    ///                                            |
    /// await TestHelper.SuspendAsync( resume => resume );
    /// </code>
    /// </para>
    /// </summary>
    /// <param name="resume">callback always called with false that completes the returned task when true is returned.</param>
    /// <returns>The task to await.</returns>
    Task SuspendAsync( Func<bool, bool> resume,
                       [CallerMemberName] string? testName = null,
                       [CallerLineNumber] int lineNumber = 0,
                       [CallerFilePath] string? fileName = null );
}
