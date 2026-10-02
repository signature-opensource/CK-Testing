# CK.Testing.Monitoring (obsolete)

This package is obsolete. Its types are now in [CK.Testing](../CK.Testing/README.md).

The last version of this package is a "tombstone":

- It contains no assembly and has no dependency.
- It ships [`buildTransitive/CK.Testing.Monitoring.targets`](buildTransitive/CK.Testing.Monitoring.targets).
  This file fails the build of any project that uses the package, directly or through another package,
  with the error `CKTESTING002`.

## Migration

1. Remove the `CK.Testing.Monitoring` package reference.
2. Reference `CK.Testing` instead. If the project references `CK.Testing.NUnit` or `CK.Testing.SqlServer`,
   you need nothing more: they bring `CK.Testing`.
3. If another package brings `CK.Testing.Monitoring` transitively, update that package.

You do not change code. The type names and the namespaces stay the same:

| Type | Namespace |
|------|-----------|
| `MonitorTestHelper` | `CK.Testing` |
| `IMonitorTestHelper` | `CK.Testing` |
| `IMonitorTestHelperCore` | `CK.Testing.Monitoring` |

`using static CK.Testing.MonitorTestHelper;` continues to work, and so do the configuration keys
(`Monitor/LogToCKMon`, `Monitor/LogToText`, `Monitor/LogLevel`, `Monitor/LogToConsole`).

A reference to the code by its assembly name must change. For example, the string
`"CK.Testing.MonitorTestHelper, CK.Testing.Monitoring"` becomes `"CK.Testing.MonitorTestHelper, CK.Testing"`.
