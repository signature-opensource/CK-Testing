Contains Sql Server test helpers.

Creates, drops, backs up and restores a real test database. The name defaults to `CKTEST_` followed by
the test project name, and the collation to `Latin1_General_100_BIN2` - case and accent sensitive, so a
test does not pass on comparisons the production schema would reject.

These operations are dangerous and no database name is protected: the drop is issued for whatever name
it is given. The `CKTEST_` prefix of the derived default is what keeps a test from reaching a real
database - configuring the name explicitly gives that up.
