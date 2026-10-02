namespace CK.Testing.SqlServer
{
    /// <summary>
    /// Read only aspect of <see cref="SqlServerDatabaseOptions"/>.
    /// Used to expose the configured information.
    /// </summary>
    public interface ISqlServerDatabaseOptions
    {
        /// <summary>
        /// Gets the database name.
        /// For the default options, this is the "SqlServer/DatabaseName" configuration entry. When it is not
        /// configured, this is "CKTEST_" followed by the test project name (<see cref="IBasicTestHelper.TestProjectName"/>):
        /// the '.' and '-' become '_' and the "Tests" part is removed ("SqlHelper.Tests" gives "CKTEST_SqlHelper").
        /// </summary>
        string DatabaseName { get; }

        /// <summary>
        /// Gets the database collation.
        /// For the default options, this is the "SqlServer/Collation" configuration entry. Defaults to 'Latin1_General_100_BIN2'.
        /// The value is sent as-is in the "create database ... collate" statement: it must be a valid SQL Server collation name.
        /// </summary>
        string Collation { get; }

        /// <summary>
        /// Gets the database compatibility level.
        /// For the default options, this is the "SqlServer/CompatibilityLevel" configuration entry.
        /// Defaults to 0: the database uses the current level of the server. The level of a server is the major of
        /// its product version multiplied by 10: it is 130 for Sql Server 2016 which product version is 13.0.
        /// </summary>
        int CompatibilityLevel { get; set; }
    }
}
