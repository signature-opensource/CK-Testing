# CK-Testing

[![Licence](https://img.shields.io/github/license/signature-opensource/CK-Testing.svg)](LICENSE)

Test helpers for the CK stack. There is one test helper, `IMonitorTestHelper`: packages extend it with
extension members, so referencing a package is what extends the test helper of a project.

| Package | Description | Latest stable |
|---------|-------------|---------------|
| [CK.Testing](CK.Testing/README.md) | The test helper: the computed paths a test needs, an ActivityMonitor for the test and its log files, and the settings of the environment. | [![nuget](https://img.shields.io/nuget/v/CK.Testing.svg?label=CK.Testing)](https://www.nuget.org/packages/CK.Testing/) |
| [CK.Testing.Monitoring](CK.Testing.Monitoring/README.md) | Obsolete: use CK.Testing. Its last version fails the build with a message that explains what to do. | [![nuget](https://img.shields.io/nuget/v/CK.Testing.Monitoring.svg?label=CK.Testing.Monitoring)](https://www.nuget.org/packages/CK.Testing.Monitoring/) |
| [CK.Testing.NUnit](CK.Testing.NUnit/README.md) | Every NUnit test logged as a group, with no code in the test project. | [![nuget](https://img.shields.io/nuget/v/CK.Testing.NUnit.svg?label=CK.Testing.NUnit)](https://www.nuget.org/packages/CK.Testing.NUnit/) |
| [CK.Testing.SqlServer](CK.Testing.SqlServer/README.md) | Create, drop, back up and restore a real test database. | [![nuget](https://img.shields.io/nuget/v/CK.Testing.SqlServer.svg?label=CK.Testing.SqlServer)](https://www.nuget.org/packages/CK.Testing.SqlServer/) |
