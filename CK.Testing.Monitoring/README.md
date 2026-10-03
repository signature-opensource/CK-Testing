# CK.Testing.Monitoring (obsolete)

This package is obsolete: use [CK.Testing](../CK.Testing/README.md).

This version of the package is a "tombstone":

- It contains no assembly and has no dependency.
- It ships [`buildTransitive/CK.Testing.Monitoring.targets`](buildTransitive/CK.Testing.Monitoring.targets).
  This file fails the build of any project that uses the package, directly or through another package,
  with the error `CKTESTING002`.

## What to do

1. Remove the `CK.Testing.Monitoring` package reference.
2. Reference `CK.Testing` instead. If the project references `CK.Testing.NUnit` or `CK.Testing.SqlServer`,
   you need nothing more: they bring `CK.Testing`.
3. If another package brings `CK.Testing.Monitoring` transitively, update that package.

`MonitorTestHelper` and `IMonitorTestHelper` are in the `CK.Testing` namespace of the `CK.Testing` assembly:
`using static CK.Testing.MonitorTestHelper;` gives the `TestHelper`. A reference to the code by its assembly
name is `"CK.Testing.MonitorTestHelper, CK.Testing"`.

## A project that also has another version

Other versions of `CK.Testing.Monitoring` contain types of `CK.Testing`, and cause `error CS0433` (the type
exists in both assemblies). To give a clear message instead, `CK.Testing` checks that no version of
`CK.Testing.Monitoring` is in the restore graph. This check fails the build with the error `CKTESTING001`.
See [CK.Testing](../CK.Testing/README.md#the-cktestingmonitoring-package-check).
