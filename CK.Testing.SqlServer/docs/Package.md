Sql Server test helpers: `SqlServerTestHelperExtensions` adds to `IMonitorTestHelper` the members that create,
drop, back up and restore a real test database (`EnsureDatabase`, `DropDatabase`, `Backup`, `GetConnectionString`).
The default database name is `CKTEST_` followed by the test project name (scoped per linked git worktree): these
operations are dangerous and no database name is protected, so prefer that default.
