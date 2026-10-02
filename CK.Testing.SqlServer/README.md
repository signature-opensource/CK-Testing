# CK.Testing.SqlServer

Test helpers for tests that need a real SQL Server database: create it, drop it, back it up,
restore it, and execute scripts on it.

## Getting the helpers.

The helpers are C# 14 extension members of `IMonitorTestHelper`, in the static class
[`SqlServerTestHelperExtensions`](SqlServerTestHelperExtensions.cs). There is no fixture base class,
no helper field and no setup method. You need two `using` directives:

```csharp
using CK.Testing;
using static CK.Testing.MonitorTestHelper;

[TestFixture]
public class DBLayerTests
{
    [Test]
    public void Execute_create_script_on_Database_and_Drop()
    {
        TestHelper.EnsureDatabase( reset: true );
        TestHelper.ExecuteScripts( File.ReadAllText( TestHelper.TestProjectFolder.AppendPart( "Model.Sql" ) ) );
        TestHelper.DropDatabase();
    }
}
```

`using static CK.Testing.MonitorTestHelper;` gives `TestHelper`. `using CK.Testing;` makes the extension
members visible. Without it, the compiler cannot find them (`error CS1061: 'IMonitorTestHelper' does not
contain a definition for 'EnsureDatabase'`). A file whose namespace is `CK.Testing` or below it does not
need the `using CK.Testing;` directive. `using static CK.Testing.SqlServerTestHelperExtensions;` also works.

The members work on any `IMonitorTestHelper`, so they also work on a mixin helper built on it (see the
`CK.Testing.Stupid` sample in the `Tests` folder).

Two behaviors that [`DBLayerTests`](../Tests/SqlHelper.Tests/DBLayerTests.cs) pins down. `DropDatabase` is
idempotent: `dropping_database_multiple_times` calls it twice after a single `EnsureDatabase` and expects no
exception. The default connection targets master:

```csharp
var c = TestHelper.MasterConnectionString;
c.ShouldContain( "master" );
c.ShouldContain( "Integrated Security" );
var c2 = TestHelper.GetConnectionString( "Toto" );
c2.ShouldContain( "Toto" );
```

The test project is named `SqlHelper.Tests`, so it uses the database `CKTEST_SqlHelper` with no configuration.

## Migration from `SqlServerTestHelper`.

The previous API (`SqlServerTestHelper`, `ISqlServerTestHelper` and `ISqlServerTestHelperCore`) is removed.
To migrate a test file:

1. Replace `using static CK.Testing.SqlServerTestHelper;` with `using static CK.Testing.MonitorTestHelper;`.
2. Add `using CK.Testing;` if the file does not have it and its namespace is not in `CK.Testing`.
3. Replace a variable or parameter of type `ISqlServerTestHelper` with `IMonitorTestHelper`.
4. Replace a subscription to `TestHelper.OnDatabaseCreatedOrDropped` with a subscription to the static
   event `SqlServerTestHelperExtensions.OnDatabaseCreatedOrDropped`.

The member names, the parameters and the call syntax (`TestHelper.EnsureDatabase()`,
`TestHelper.MasterConnectionString`, `TestHelper.Backup`) do not change.

## Nothing protects any database name.

`DropDatabase` builds this statement for any name that it receives:

```csharp
var exec = $"if db_id('{dbName}') is not null begin ";
if( closeExistingConnections )
{
    exec += $"alter database [{dbName}] set single_user with rollback immediate;";
}
exec += $"drop database [{dbName}]; select 1; end else begin select 0; end";
```

No code compares the name with `master`, `tempdb`, `model` or any other name. The `if db_id(...) is not null`
guard makes a second drop do nothing, instead of an error. The `CKTEST_` prefix of the default name (see
below) is the only safeguard.

## The default database name is prefixed, and that is the real safety net.

When `SqlServer/DatabaseName` is not configured, the name derives from the test project name:

```csharp
var n = "CKTEST_" + testProjectName.Replace( '.', '_' ).Replace( '-', '_' );
var dbName = n.Replace( "_Tests", String.Empty );
if( dbName == n ) dbName = n.Replace( "Tests", String.Empty );
```

`SqlHelper.Tests` therefore gives **`CKTEST_SqlHelper`**. Two consequences:

- Two test projects can run side by side with no configuration, because each one has its own name.
- A default test database cannot have the name of a real database: the `CKTEST_` prefix prevents it.
  **If you configure `SqlServer/DatabaseName`, you remove that protection**: the configured value is used
  as-is, and one call drops it.

The method `GetDefaultDatabaseName` in `SqlServerTestHelperExtensions` is the only place that computes this name.

## Configuration.

| Member | Configuration key | Default |
|--------|-------------------|---------|
| `MasterConnectionString` | `SqlServer/MasterConnectionString` | `Server=.;Database=master;Integrated Security=SSPI;TrustServerCertificate=True` |
| `DefaultDatabaseOptions.DatabaseName` | `SqlServer/DatabaseName` | `CKTEST_` + the test project name, see above |
| `DefaultDatabaseOptions.Collation` | `SqlServer/Collation` | `Latin1_General_100_BIN2` |
| `DefaultDatabaseOptions.CompatibilityLevel` | `SqlServer/CompatibilityLevel` | `0` |

The helpers read these keys once per process, from `TestHelperConfiguration.Default`, when a member is used
for the first time. The values are kept in static fields. A `TestHelperConfiguration` throws if a key is
declared two times, so no other code must declare these keys.

`TrustServerCertificate=True` in the default is necessary with `Microsoft.Data.SqlClient`: without it, the
client refuses a local server with a self-signed certificate. The `MasterConnectionString` property returns
the normalized output of a `SqlConnectionStringBuilder`, not the literal string above.

`CompatibilityLevel` **is** `0` by default, not the server level. `0` means "use the current level of the
server" when `EnsureDatabase` creates a database. So the value that you read from the options is `0`, and
the database gets the level of the server.

The collation is sent as-is in `create database ... collate {Collation}`. It must be a valid SQL Server
collation name.

Changing `Collation` has an effect: `EnsureDatabase` **drops and creates again** a database when its
collation is not the expected one.

## Reading the state.

`GetConnectionString( databaseName = null )` builds a connection string from `MasterConnectionString`.
The default database is the default target.

`GetDatabaseOptions( name )` returns null when the database does not exist. Be careful with a null argument:
`GetDatabaseOptions( null )` returns a copy of the *default* options, not null. You cannot use it to find
if the default database exists: give the name.

## Events and backups.

`SqlServerTestHelperExtensions.OnDatabaseCreatedOrDropped` is a **static** event: C# does not allow an event
in an extension block. It fires on creation, on reset **and on drop**. The argument has `CreatedOrReset` and
`Dropped`, so a handler can tell which operation occurred. The sender is the `IMonitorTestHelper` that did the
operation. A fixture can therefore seed a new database in one place, instead of in each test.

The type of the argument is `SqlServerDatabaseEventArgs`. Its file has another name,
[SqlServerDatabaseCreatedEventArgs.cs](SqlServerDatabaseCreatedEventArgs.cs).

`Backup` gives a [`BackupManager`](BackupManager.cs) for the backup and restore cycle. The backups go to the
`DBBackup` folder of the test project. The explicit tests of `DBLayerTests` that use it depend on their order:
`backup_create` fails if the database does not exist, and `backup_restore` fails if no backup exists.

## Requires.

- `CK.Testing` (the monitor test helper; operations are logged), `Microsoft.Data.SqlClient`.
- C# 14 (the default for `net10.0`) in this project. A consumer that sets an older `LangVersion` can call
  the extension methods (`EnsureDatabase`, `ExecuteScripts`, ...), but not the extension properties
  (`MasterConnectionString`, `DefaultDatabaseOptions`, `Backup`): the compiler gives `error CS9058: Feature
  'extensions' is not available in C# 11.0`.
