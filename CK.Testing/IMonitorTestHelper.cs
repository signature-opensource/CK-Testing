using CK.Core;
using CK.Core.Json;
using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace CK.Testing;

/// <summary>
/// The test helper: the paths a test needs, a monitor and its log files, and some utilities.
/// <para>
/// Use <c>using static CK.Testing.MonitorTestHelper;</c> to get the <see cref="MonitorTestHelper.TestHelper"/>.
/// Other packages extend it with extension members of this interface.
/// </para>
/// </summary>
public interface IMonitorTestHelper
{
    /// <summary>
    /// Gets the build configuration ("Debug" or "Release").
    /// </summary>
    string BuildConfiguration { get; }

    /// <summary>
    /// Gets the name of the running test project (the last part of <see cref="TestProjectFolder"/>).
    /// </summary>
    string TestProjectName { get; }

    /// <summary>
    /// Gets the name of the Solution. This is usually the last part of <see cref="SolutionFolder"/>.
    /// In a linked git worktree, this is the name of the main repository: the worktree folder can have any name.
    /// <para>
    /// This is the name found by <see cref="LocalDevSolution.TryFindSolutionFolder(string, out NormalizedPath, out string?, out string?)"/>.
    /// </para>
    /// </summary>
    string SolutionName { get; }

    /// <summary>
    /// Gets the solution folder: the git working folder that contains the test project.
    /// This is the folder that contains the ".git" directory of a main checkout, or the ".git" file
    /// of a linked git worktree or of a git submodule.
    /// <para>
    /// In a linked git worktree, this is the worktree folder, not the folder of the main checkout.
    /// A git submodule is its own solution: this is the submodule folder, not the folder of the superproject.
    /// The search is <see cref="LocalDevSolution.TryFindSolutionFolder(string, out NormalizedPath, out string?, out string?)"/>,
    /// from the <see cref="BinFolder"/>.
    /// </para>
    /// </summary>
    NormalizedPath SolutionFolder { get; }

    /// <summary>
    /// Gets the path to the test project folder (where the .csproj is).
    /// This is usually where folders specific to the test should be created and managed (like a
    /// "TestScripts" folder).
    /// The <see cref="LogFolder"/> is located inside this one.
    /// </summary>
    NormalizedPath TestProjectFolder { get; }

    /// <summary>
    /// Tries to locate the SUT (System Under Test) project based on the <see cref="TestProjectName"/> (if it ends with ".Tests"):
    /// it is the first directory that exists in a directory above with a name without the ".Tests" suffix.
    /// A directory with the ".SUT" suffix instead of ".Tests" has the priority.
    /// If no matching directory is found, this fallbacks to <see cref="TestProjectFolder"/>.
    /// </summary>
    NormalizedPath ClosestSUTProjectFolder { get; }

    /// <summary>
    /// Gets the path to the log folder. It is the 'Logs' folder in the <see cref="TestProjectFolder"/>.
    /// </summary>
    NormalizedPath LogFolder { get; }

    /// <summary>
    /// Gets the bin folder where the tests are being executed.
    /// This normally is the same as <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    NormalizedPath BinFolder { get; }

    /// <summary>
    /// Gets the sub path from <see cref="TestProjectFolder"/> to <see cref="BinFolder"/>.
    /// This captures the "bin/<see cref="BuildConfiguration"/>}/(target framework folder)"/>.
    /// </summary>
    NormalizedPath PathToBin { get; }

    /// <summary>
    /// Gets a setting from the environment: the value of the environment variable "TestHelper__" followed by the
    /// <paramref name="key"/> where each '/' is replaced by "__". The key "SqlServer/MasterConnectionString" is
    /// read from the "TestHelper__SqlServer__MasterConnectionString" environment variable.
    /// <para>
    /// A setting is for a value that depends on the machine (a server, a credential) or that a developer
    /// changes without changing the code.
    /// </para>
    /// </summary>
    /// <param name="key">The setting key: parts separated by '/'.</param>
    /// <returns>The value or null if the environment variable is not defined or empty.</returns>
    string? GetSetting( string key );

    /// <summary>
    /// Gets the monitor.
    /// </summary>
    IActivityMonitor Monitor { get; }

    /// <summary>
    /// Gets or sets whether <see cref="Monitor"/> will log into the console.
    /// The initial value is the "Monitor/LogToConsole" setting (see <see cref="GetSetting(string)"/>). It defaults to false.
    /// </summary>
    bool LogToConsole { get; set; }

    /// <summary>
    /// Gets whether all activities are logged to <see cref="LogFolder"/>/CKMon folders.
    /// This is the "Monitor/LogToCKMon" setting (see <see cref="GetSetting(string)"/>). It defaults to true.
    /// </summary>
    bool LogToCKMon { get; }

    /// <summary>
    /// Gets whether all activities are logged to <see cref="LogFolder"/>/Text folders.
    /// This is the "Monitor/LogToText" setting (see <see cref="GetSetting(string)"/>). It defaults to true.
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
    /// This can be used only when <see cref="System.Diagnostics.Debugger.IsAttached"/> is true: this is ignored otherwise.
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
    /// <param name="testName">Name of the calling method, automatically sets by the compiler.</param>
    /// <param name="lineNumber">Line number in the source file, automatically sets by the compiler.</param>
    /// <param name="fileName">Path of the source file, automatically sets by the compiler.</param>
    /// <returns>The task to await.</returns>
    Task SuspendAsync( Func<bool, bool> resume,
                       [CallerMemberName] string? testName = null,
                       [CallerLineNumber] int lineNumber = 0,
                       [CallerFilePath] string? fileName = null );

    /// <summary>
    /// Clears a folder from all its existing content or ensures it exists
    /// and that a file can be written in it, or simple destroys it.
    /// The cleanup is logged in the <see cref="Monitor"/>.
    /// </summary>
    /// <param name="folder">The path to the folder.</param>
    /// <param name="ensureFolderAvailable">
    /// By default, ensures that the folder exists and clears is content.
    /// When false, the folder and its content is removed.
    /// </param>
    /// <param name="maxRetryCount">Maximal number of retries on failure.</param>
    /// <returns>The <paramref name="folder"/>.</returns>
    NormalizedPath CleanupFolder( NormalizedPath folder, bool ensureFolderAvailable = true, int maxRetryCount = 5 );

    /// <summary>
    /// Executes an action once and only the first time it is called during the application lifetime.
    /// The action is identified by the calling site.
    /// </summary>
    /// <param name="a">Action to execute.</param>
    /// <param name="s">Path of the source file, automatically sets by the compiler.</param>
    /// <param name="l">Line number in the source file, automatically sets by the compiler.</param>
    void OnlyOnce( Action a, [CallerFilePath] string? s = null, [CallerLineNumber] int l = 0 );

    /// <summary>
    /// Writes a <typeparamref name="T"/> instance, reads it back and writes the result, ensuring that
    /// the two json string are equals. Throws a <see cref="CKException"/> if the texts differ.
    /// </summary>
    /// <typeparam name="T">The type of the instance to check.</typeparam>
    /// <param name="o">The instance.</param>
    /// <param name="write">Writer function. This is called twice unless the first write or the read fails.</param>
    /// <param name="read">Reader function is called once.</param>
    /// <param name="readerContext">Optional reader context. Defaults to <see cref="IUtf8JsonReaderContext.Empty"/>.</param>
    /// <param name="jsonText1">Optional hook that provides the Json text.</param>
    /// <param name="jsonText2">Optional hook that provides the second Json text. This is set before throwing if it differs from <paramref name="jsonText1"/>.</param>
    /// <returns>A clone of <paramref name="o"/>.</returns>
    T JsonIdempotenceCheck<T>( T o,
                               Action<Utf8JsonWriter, T> write,
                               Utf8JsonReaderDelegate<T> read,
                               IUtf8JsonReaderContext? readerContext = null,
                               Action<string>? jsonText1 = null,
                               Action<string>? jsonText2 = null );

    /// <summary>
    /// Writes a <typeparamref name="T"/> instance, reads it back and writes the result, ensuring that
    /// the two json string are equals. Throws a <see cref="CKException"/> if the texts differ.
    /// </summary>
    /// <typeparam name="T">The type of the instance to check.</typeparam>
    /// <typeparam name="TReadContext">The read context.</typeparam>
    /// <param name="o">The instance.</param>
    /// <param name="write">Writer function. This is called twice unless the first write or the read fails.</param>
    /// <param name="read">Reader function is called once.</param>
    /// <param name="readerContext">Reader context.</param>
    /// <param name="jsonText1">Optional hook that provides the first Json text.</param>
    /// <param name="jsonText2">Optional hook that provides the second Json text. This is set before throwing if it differs from <paramref name="jsonText1"/>.</param>
    /// <returns>A clone of <paramref name="o"/>.</returns>
    T JsonIdempotenceCheck<T, TReadContext>( T o,
                                             Action<Utf8JsonWriter, T> write,
                                             Utf8JsonReaderDelegate<T, TReadContext> read,
                                             TReadContext readerContext,
                                             Action<string>? jsonText1 = null,
                                             Action<string>? jsonText2 = null )
        where TReadContext : class, IUtf8JsonReaderContext;
}
