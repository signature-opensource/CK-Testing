Makes every NUnit test log itself as a group into the `TestHelper` monitor, with nothing to write in the test
project: MSBuild files shipped in `buildTransitive` generate the assembly attribute `TestHelperMonitorSupportAttribute`.
Other CK.Testing packages plug their own NUnit attributes in by appending to the same MSBuild property.
