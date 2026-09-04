# CK.Testing.SqlServer

Test helper mixin for tests that need a real SQL Server database: create it, drop it, back it up,
restore it.

## Getting the helper.

One `using static`, and there is no fixture base class, no helper field, no setup method:

```csharp
using static CK.Testing.SqlServerTestHelper;

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

`TestHelper` here carries this package's members *and* those of the helpers it is mixed with -
`TestProjectFolder` above comes from `IBasicTestHelper`, `Monitor` from `CK.Testing.Monitoring`.

Two behaviours the same fixture pins down. `DropDatabase` is idempotent - `dropping_database_multiple_times`
calls it twice after a single `EnsureDatabase` and expects no throw - and the default connection targets
master:

```csharp
var c = TestHelper.MasterConnectionString;
c.ShouldContain( "master" );
c.ShouldContain( "Integrated Security" );
var c2 = TestHelper.GetConnectionString( "Toto" );
c2.ShouldContain( "Toto" );
```

From [`DBLayerTests`](../Tests/SqlHelper.Tests/DBLayerTests.cs). That project is also the worked example
of the section below: being named `SqlHelper.Tests`, it operates on `CKTEST_SqlHelper` without
configuring anything.

## Nothing protects any database name.

[`ISqlServerTestHelperCore`](ISqlServerTestHelperCore.cs) opens with *"Support sql database related
helpers."*, then:

```
/// Operations exposed here are dangerous. The only check is that the database name can not be
/// system databases like 'master', 'tempdb' or 'model'.
```

**That check does not exist.** `DoDrop` in [`SqlServerTestHelper`](SqlServerTestHelper.cs) builds

```csharp
var exec = $"if db_id('{dbName}') is not null begin ";
if( closeExistingConnections )
{
    exec += $"alter database [{dbName}] set single_user with rollback immediate;";
}
exec += $"drop database [{dbName}]; select 1; end else begin select 0; end";
```

for whatever name it is given, and neither it nor `DoEnsureDatabase` compares the name against
anything. The `if db_id(...) is not null` guard is what makes a second drop a no-op rather than an
error, and the `alter database` clause is conditional on `closeExistingConnections`. `master` appears
nowhere as a guard - only in a connection string, in a `Declare` description, in the `use [master]` of
[`BackupManager`](BackupManager.cs), and in four doc comments.

So the first half of that comment is the operative part: these operations are dangerous, and the
safeguard is the naming convention below, not a guard.

## The default database name is prefixed, and that is the real safety net.

When `SqlServer/DatabaseName` is not configured, the name is derived from the test project name:

```csharp
var n = "CKTEST_" + _monitor.TestProjectName.Replace( '.', '_' ).Replace( '-', '_' );
dbName = n.Replace( "_Tests", String.Empty );
if( dbName == n ) dbName = n.Replace( "Tests", String.Empty );
```

`SqlHelper.Tests` therefore yields **`CKTEST_SqlHelper`**, not `SqlHelper.Tests`. Two consequences:

- Two test projects run side by side with nothing configured, because the name is derived.
- A default-named test database cannot collide with a real one - the `CKTEST_` prefix is what keeps
  the missing guard from mattering. **Configuring `SqlServer/DatabaseName` removes that protection**:
  the configured value is used verbatim, and dropping it is one call away.

## Configuration.

| Member | Configuration key | Default |
|--------|-------------------|---------|
| `MasterConnectionString` | `SqlServer/MasterConnectionString` | `Server=.;Database=master;Integrated Security=SSPI;TrustServerCertificate=True` |
| `DefaultDatabaseOptions.DatabaseName` | `SqlServer/DatabaseName` | `CKTEST_` + the test project name, see above |
| `DefaultDatabaseOptions.Collation` | `SqlServer/Collation` | `Latin1_General_100_BIN2` |
| `DefaultDatabaseOptions.CompatibilityLevel` | `SqlServer/CompatibilityLevel` | `0` |

`TrustServerCertificate=True` in the default is load-bearing with `Microsoft.Data.SqlClient` 6.1.1 -
without it a local server with a self-signed certificate is refused. Note that the
`MasterConnectionString` property returns `EnsureMasterConnection().ToString()`, a normalized builder
output, not the literal string above.

`CompatibilityLevel` **is** `0` by default, not the server level: the key is declared with no default
value. `0` is a sentinel that `DoEnsureDatabase` interprets at creation time as "leave the server
current level alone" - so the value you read from the options is `0`, and the level the database ends
up with is the server's.

Changing `Collation` is not inert: the declared description states that `EnsureDatabase` **drops and
recreates** the database when the collation differs from the configured one.

## Reading the state.

`GetConnectionString( databaseName = null )` builds a connection string from `MasterConnectionString`,
defaulting to the default database.

`GetDatabaseOptions( name )` returns null when the database does not exist. Careful with the null
argument: `GetDatabaseOptions( null )` returns the *default* options rather than null, so it cannot be
used to test whether the default database exists - pass the name.

## Events and backups.

`event EventHandler<SqlServerDatabaseEventArgs>? OnDatabaseCreatedOrDropped` fires on creation, on
reset **and on drop** - the argument carries `CreatedOrReset` and `Dropped` so a handler can tell
which. A fixture can therefore seed a freshly created database in one place instead of in every test.

Note the type is `SqlServerDatabaseEventArgs`, although it is declared in a file named
[SqlServerDatabaseCreatedEventArgs.cs](SqlServerDatabaseCreatedEventArgs.cs) - the file name is stale.

`Backup` exposes a [`BackupManager`](BackupManager.cs) for the backup and restore cycle.

## Requires.

- `CK.Testing.Monitoring` (operations are logged), `Microsoft.Data.SqlClient`.

## Stale XML comments in this package.

Five doc comments contradict the code. They are listed here because they are what a reader meets
first, and each one was believed once already:

| Comment | Reality |
|---------|---------|
| `ISqlServerTestHelperCore.cs` - "the only check is that the database name can not be system databases" | there is no name check at all |
| `ISqlServerTestHelperCore.cs` - master connection defaults to `...Integrated Security=true` | `...Integrated Security=SSPI;TrustServerCertificate=True` |
| `ISqlServerDatabaseOptions.cs` - `DatabaseName` defaults to the test project name | it defaults to `CKTEST_` + that name, with `Tests` stripped |
| `ISqlServerDatabaseOptions.cs` - `CompatibilityLevel` reads `"SqlServer/Collation"` | the key is `SqlServer/CompatibilityLevel` |
| `ISqlServerDatabaseOptions.cs` / `SqlServerDatabaseOptions.cs` - `Collation` accepts `'Random'` to pick a random collation | not implemented: the value is interpolated straight into `create database ... collate {Collation}`, so `collate Random` is sent to the server and fails |
