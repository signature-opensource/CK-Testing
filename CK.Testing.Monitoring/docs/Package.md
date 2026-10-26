Obsolete: use CK.Testing. This package contains no assembly and fails the build of any project that uses it,
directly or transitively, with the error `CKTESTING002`.
Replace the reference with `CK.Testing` (also brought by `CK.Testing.NUnit` and `CK.Testing.SqlServer`), or update
the package that brings it. `MonitorTestHelper` and `IMonitorTestHelper` are in the `CK.Testing` assembly.
