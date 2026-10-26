The CK stack test helper: `IMonitorTestHelper`, imported through `using static CK.Testing.MonitorTestHelper;`,
gives the computed paths a test needs, the environment settings (`GetSetting`) and an `IActivityMonitor` whose
`GrandOutput` writes text and `.ckmon` logs. Other packages extend it with extension members of
`IMonitorTestHelper`. Assertions use Shouldly, extended by `CKShouldlyExtensions`.
