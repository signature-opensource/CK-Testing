Contains Test Helpers mixin infrastructure and the Monitor Test Helper. Shouldly is used for assertions.

A test helper is an interface that the resolver turns into an instance: a class named by convention is
used when one exists, otherwise a mixin interface - one that declares no member of its own - gets an
implementation emitted at runtime. A package can therefore extend the test helper of a project without
that project changing anything.

Also brings the layered test configuration - every `*.TestHelper.config` then `TestHelper.config` from
the solution folder down to the execution path, then environment variables prefixed `TestHelper::` or
`TestHelper__` - and the computed paths a test needs, including the folder of the project under test.

The Monitor Test Helper brings an `IActivityMonitor` to a test, imported through
`using static CK.Testing.MonitorTestHelper;`, and activates the `GrandOutput` that collects it: text and
binary `.ckmon` handlers are added according to the test configuration, and write under the test log
folder. This helper was in the `CK.Testing.Monitoring` package before: that package is now obsolete.
