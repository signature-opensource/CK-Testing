Contains Monitoring Test Helper mixin.

Brings an `IActivityMonitor` to a test, imported through `using static CK.Testing.MonitorTestHelper;`,
and activates the `GrandOutput` that collects it: text and binary `.ckmon` handlers are added according
to the test configuration, writing under the test log folder.

Console output is the one switch a test can flip for itself, either for its whole duration or scoped to
a `using` block.
