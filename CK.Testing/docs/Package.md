Contains the Monitor Test Helper. Shouldly is used for assertions.

There is one test helper, `IMonitorTestHelper`, imported through `using static CK.Testing.MonitorTestHelper;`.
Other packages extend it with extension members of `IMonitorTestHelper`, so referencing them is enough to see
their members on `TestHelper`.

The test helper gives the computed paths a test needs, including the folder of the project under test, and the
settings of the environment: `GetSetting( "Monitor/LogLevel" )` reads the `TestHelper__Monitor__LogLevel`
environment variable.

It brings an `IActivityMonitor` to a test and activates the `GrandOutput` that collects it: text and binary
`.ckmon` handlers are added according to the settings, and write under the test log folder.
