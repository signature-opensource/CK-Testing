Obsolete: the Monitor Test Helper is now in CK.Testing.

This package contains no assembly and has no dependency. It fails the build of any project that uses it,
directly or through another package, with the error `CKTESTING002`.

To migrate:

1. Remove the `CK.Testing.Monitoring` package reference.
2. Reference `CK.Testing` instead. `CK.Testing.NUnit` and `CK.Testing.SqlServer` bring `CK.Testing`.
3. If another package brings `CK.Testing.Monitoring` transitively, update that package.

The type names and the namespaces do not change: `MonitorTestHelper` and `IMonitorTestHelper` are in
`CK.Testing`, `IMonitorTestHelperCore` is in `CK.Testing.Monitoring`. Only a reference by assembly name
changes: `"CK.Testing.MonitorTestHelper, CK.Testing.Monitoring"` becomes `"CK.Testing.MonitorTestHelper, CK.Testing"`.
