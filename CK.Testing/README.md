# CK.Testing: one test helper, extended by extension members

There is one test helper: [`IMonitorTestHelper`](IMonitorTestHelper.cs). It gives the paths a test needs,
an `IActivityMonitor` with its log files, the settings of the environment and some utilities.
[`MonitorTestHelper`](MonitorTestHelper.cs) gives the only instance:

```csharp
using static CK.Testing.MonitorTestHelper;
// ...
TestHelper.Monitor.Info( "..." );
```

All the CK stack uses this `using static`. `TestHelper` is not a field that you declare.

Assertions use [Shouldly](https://docs.shouldly.org/), extended by
[`CKShouldlyExtensions`](CKShouldlyExtensions.cs). String comparisons are ordinal by default (Shouldly
ignores the case to find a substring, a start or an end, and orders strings with the current culture), and the
assertions that constrain their subject without fully defining it return it, so that they can be chained:

```csharp
"Hello World!".ShouldStartWith( "Hello" ).ShouldEndWith( "!" );
3712.ShouldBePositive().ShouldBeLessThan( 5000 );
```

## A package extends the test helper with extension members.

A package that adds capabilities to the test helper writes extension members of `IMonitorTestHelper`, and
keeps its state in static fields. There is nothing else to write: no interface to implement, no class to
register. Referencing the package (and having its namespace in scope) is what makes its members appear on
`TestHelper`. [`CK.Testing.SqlServer`](../CK.Testing.SqlServer/README.md) is built this way, and so is the
[`CK.Testing.Stupid`](../Tests/CK.Testing.Stupid/StupidTestHelperExtensions.cs) sample:

```csharp
public static class StupidTestHelperExtensions
{
    static int _countCall;

    // An event cannot be declared in an extension block: it is a static event.
    public static event EventHandler? OnStupidMethodCalled;

    extension( IMonitorTestHelper helper )
    {
        public int CountOfStupidMethodCalls => _countCall;

        public void StupidMethod()
        {
            ++_countCall;
            OnStupidMethodCalled?.Invoke( null, EventArgs.Empty );
        }
    }
}
```

Static state is correct because there is only one test helper per process. An extension that needs an
initialization can do it in the static constructor of its class: it runs before the first use of any of its
members. An extension that needs a value from the machine reads a [setting](#settings-are-environment-variables).

The `extension` blocks need C# 14 (the default for `net10.0`). Classic extension methods
(`this IMonitorTestHelper helper`) work too.

## The test helper is created once.

`MonitorTestHelper.TestHelper` creates the helper on its first access. Before this, a static initialization
computes the paths and checks the folder layout (see below). If it fails, every access to `TestHelper` throws
the initialization error. If the creation fails (an invalid setting, for example), the next access tries again.

## Where a test helper knows it is.

`IMonitorTestHelper` exposes the paths a test needs: `SolutionFolder`,
`TestProjectFolder`, `ClosestSUTProjectFolder` (the project under test), `BinFolder`, `PathToBin`,
`LogFolder`, `BuildConfiguration`, `TestProjectName` and `SolutionName`.

`SolutionFolder` is the git working folder that contains the test project. `LocalDevSolution.TryFindSolutionFolder`
(in the `CK.ActivityMonitor` package) finds it from the `BinFolder`. It is the folder that contains:

- the `.git` directory of a main checkout;
- the `.git` file of a linked git worktree: `SolutionFolder` is the worktree folder, not the main checkout;
- the `.git` file of a git submodule: a submodule is its own solution, not the superproject.

A `.git` file that does not point to a git directory is ignored, and the search continues above it.

`SolutionName` is the last part of `SolutionFolder`, except in a linked worktree: it is then the name of the
main repository, because a worktree folder can have any name.

The initialization also checks that the bin folder is below a `Debug` or `Release` folder and a `bin` folder,
and that a `Tests` folder is above the test project. This `Tests` folder is usually in the solution. When the
solution is a submodule (often in the `Tests` folder of its superproject), a `Tests` folder of the superproject,
between the submodule and the superproject folder, is also accepted. This repeats for nested submodules. No
folder above a main checkout or a linked worktree is accepted. The solution folder must not be a root (`/`, a
drive or a UNC share); a folder like `/src` (a Docker `WORKDIR`) is accepted. If a check fails, every test fails
with the initialization error.

`ClosestSUTProjectFolder` is the one worth knowing. For a test project whose name ends with `.Tests`, it is the
closest folder with the same name without `.Tests`, searched upward as far as the solution folder, siblings
first. A `<Name>.SUT` folder has the priority, wherever it is. When no folder is found, this is the
`TestProjectFolder`. So a fixture can reach the real sources without a relative path hard-coded in the test.

## Settings are environment variables.

`TestHelper.GetSetting( key )` returns the value of the environment variable `TestHelper__` followed by the key,
where each `/` is replaced by `__`. The key `Monitor/LogLevel` is the environment variable `TestHelper__Monitor__LogLevel`.
An empty variable is the same as no variable: `GetSetting` returns null.

A setting is for a value that depends on the machine (a server, a credential) or that a developer changes
without changing the code. A test runner reads the environment variables when it starts: after a change,
restart it (or the IDE).

## The monitor.

The test helper brings an ActivityMonitor to a test, and sets up the `GrandOutput` that collects its logs.

| Member of `IMonitorTestHelper` | |
|--------|--|
| `IActivityMonitor Monitor { get; }` | The monitor that a test logs into. |
| `bool LogToConsole { get; set; }` | The only settable member: a test can send its logs to the console. |
| `bool LogToCKMon { get; }` | Binary `.ckmon` output, from the settings. |
| `bool LogToText { get; }` | Text file output, from the settings. |
| `IDisposable TemporaryEnsureConsoleMonitor()` | Console logs until the returned object is disposed. Dispose restores the previous value. |
| `Task SuspendAsync( Func<bool,bool> resume, ... )` | Suspends the test until the callback returns true. It works only when a debugger is attached. |

The other members are utilities: `CleanupFolder` (logged in the monitor), `OnlyOnce` and `JsonIdempotenceCheck`.

### It owns the GrandOutput of the test run.

When the test helper is created, it sets `LogFile.RootLogPath` to the `LogFolder`, builds a
`GrandOutputConfiguration`, adds the handlers that the settings ask for, and calls
`GrandOutput.EnsureActiveDefault`.

The output paths are fixed. The settings only select which handlers exist:

| Setting | Effect | Where it writes |
|---------|--------|-----------------|
| `Monitor/LogToCKMon` | `true` or `false`. Adds a `BinaryFileConfiguration` with gzip compression. Default: true. | `<LogFolder>/CKMon` |
| `Monitor/LogToText` | `true` or `false`. Adds a `TextFileConfiguration`. Default: true. | `<LogFolder>/Text` |
| `Monitor/LogToConsole` | `true` or `false`. Initial value of the `LogToConsole` property. Default: false. | console |
| `Monitor/LogLevel` | A `LogFilter` that sets `ActivityMonitor.DefaultFilter`. Default: `Debug`. | - |

Another value than `true` or `false` makes the creation of the test helper fail.

Both handlers use a timed-folder mode with a maximum count of current folders (5) and archived folders
(20). This limit prevents a long test suite from filling the disk.

`MonitorTestHelper` activates `GrandOutput.Default`. If a test project also configures the
`GrandOutput`, the last call to `EnsureActiveDefault` wins.

### The CK.Testing.Monitoring package check.

The `CK.Testing.Monitoring` package is obsolete: its last version contains no assembly, and it fails the build
with the error `CKTESTING002`. See its [README](../CK.Testing.Monitoring/README.md).

Other versions of `CK.Testing.Monitoring` contain the same types as this package. If a project gets
both, the compiler fails with `error CS0433` (the type exists in both assemblies), and only where the
code uses the type. To give a clear message instead, this package ships
[`buildTransitive/CK.Testing.targets`](buildTransitive/CK.Testing.targets). This check fails the build
with the error `CKTESTING001` when any version of `CK.Testing.Monitoring` is in the restore graph,
directly or transitively.

To fix the error, remove the `CK.Testing.Monitoring` package reference, or update the package that
brings it. If you cannot do this (for example, a package that nobody republishes brings it), set this
property in the project to skip the check:

```xml
<PropertyGroup>
  <CKTestingAllowObsoleteMonitoringPackage>true</CKTestingAllowObsoleteMonitoringPackage>
</PropertyGroup>
```

`ExcludeAssets="all"` on the reference does not remove the package from the restore graph, so it does not
skip the check. After you skip the check, you must also solve the duplicate types yourself, for example
with `ExcludeAssets="compile"` on a direct reference to the package, or with an `extern alias`.
