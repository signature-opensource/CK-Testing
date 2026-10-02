using System;

namespace CK.Testing.SqlServer
{
    /// <summary>
    /// Argument of the <see cref="SqlServerTestHelperExtensions.OnDatabaseCreatedOrDropped"/> event.
    /// </summary>
    public class SqlServerDatabaseEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new <see cref="SqlServerDatabaseEventArgs"/>.
        /// </summary>
        /// <param name="options">Database options.</param>
        /// <param name="dropped">Whether it has been dropped or created/reset.</param>
        public SqlServerDatabaseEventArgs( ISqlServerDatabaseOptions options, bool dropped )
        {
            DatabaseOptions = options;
            Dropped = dropped;
        }

        /// <summary>
        /// Gets the database options.
        /// </summary>
        public ISqlServerDatabaseOptions DatabaseOptions { get; }

        /// <summary>
        /// Gets whether the database has been created or reset.
        /// </summary>
        public bool CreatedOrReset => !Dropped;

        /// <summary>
        /// Gets whether the database has been dropped.
        /// </summary>
        public bool Dropped { get; }
    }
}
