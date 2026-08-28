# CK.Testing.Monitoring

The test helper mixin that brings an ActivityMonitor to a test, and sets up the `GrandOutput` that
collects it.

## What it brings.

[`IMonitorTestHelperCore`](IMonitorTestHelperCore.cs):

| Member | |
|--------|--|
| `IActivityMonitor Monitor { get; }` | the monitor a test logs into |
| `bool LogToConsole { get; set; }` | the only settable one - a test can turn the console on for itself |
| `bool LogToCKMon { get; }` | binary `.ckmon` output, from configuration |
| `bool LogToText { get; }` | text file output, from configuration |
| `IDisposable TemporaryEnsureConsoleMonitor()` | console logging for the duration of a `using`; the previous value is restored on dispose |
| `Task SuspendAsync( Func<bool,bool> resume, ... )` | suspends the test until the callback allows it to resume |

[`MonitorTestHelper`](MonitorTestHelper.cs) is the static entry point, meant to be imported:

```csharp
using static CK.Testing.MonitorTestHelper;
// ...
TestHelper.Monitor.Info( "..." );
```

That `using static` is the idiom across the whole stack - `TestHelper` is not a field you declare.

## It owns the GrandOutput of the test run.

This is not a passive mixin. [`MonitorTestHelper`](MonitorTestHelper.cs) sets
`LogFile.RootLogPath = basic.LogFolder`, builds a `GrandOutputConfiguration`, adds the handlers the
configuration asked for, and calls `GrandOutput.EnsureActiveDefault`.

The output paths are **fixed**, not configurable - only whether each handler exists is:

| Configuration key | Effect | Where it writes |
|-------------------|--------|-----------------|
| `Monitor/LogToCKMon` | adds a `BinaryFileConfiguration`, gzip compressed | `<LogFolder>/CKMon` |
| `Monitor/LogToText` | adds a `TextFileConfiguration` | `<LogFolder>/Text` |
| `Monitor/LogToConsole` | initial value of the settable property | console |
| `Monitor/LogLevel` | sets `ActivityMonitor.DefaultFilter`, defaults to `Debug` | - |

Both file keys accept deprecated aliases (`Monitor/LogToBinFile`, `Monitor/LogToBinFiles`,
`Monitor/LogToTextFile`, `Monitor/LogToTextFiles`), so an old `TestHelper.config` keeps working.

Both handlers are created with a timed-folder mode bounded by a maximum count of current and archived
folders, which is what stops a long test suite from filling the disk.

## Requires.

- `CK.Testing` (the mixin infrastructure), `CK.Monitoring` and `CK.ActivityMonitor.SimpleSender`.

Because this package activates `GrandOutput.Default`, a test project that also configures the
GrandOutput itself is configuring it twice - the last `EnsureActiveDefault` wins.
