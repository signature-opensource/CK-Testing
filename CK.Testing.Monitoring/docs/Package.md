Obsolete: use CK.Testing.

This package contains no assembly and has no dependency. It fails the build of any project that uses it,
directly or through another package, with the error `CKTESTING002`.

What to do:

1. Remove the `CK.Testing.Monitoring` package reference.
2. Reference `CK.Testing` instead. `CK.Testing.NUnit` and `CK.Testing.SqlServer` bring `CK.Testing`.
3. If another package brings `CK.Testing.Monitoring` transitively, update that package.

`MonitorTestHelper` and `IMonitorTestHelper` are in the `CK.Testing` namespace of the `CK.Testing` assembly.
A reference by assembly name is `"CK.Testing.MonitorTestHelper, CK.Testing"`.
