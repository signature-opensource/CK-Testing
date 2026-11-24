The CK stack test helper: `IMonitorTestHelper` gives a test the computed paths it needs, the environment settings and
an `IActivityMonitor` that writes text and `.ckmon` logs. Assertions use Shouldly.

`IMonitorTestHelper` is imported through `using static CK.Testing.MonitorTestHelper;`, the settings are read by
`GetSetting` and the logs are written by a `GrandOutput`. Other packages extend it with extension members of
`IMonitorTestHelper`. Shouldly is extended by `CKShouldlyExtensions`.
