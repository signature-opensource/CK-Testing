using CK.Testing.SqlServer;
using System;

namespace CK.Testing
{
    /// <summary>
    /// Encapsulates database options.
    /// </summary>
    public class SqlServerDatabaseOptions : ISqlServerDatabaseOptions
    {
        /// <summary>
        /// Initializes a new <see cref="SqlServerDatabaseOptions"/>. <see cref="CompatibilityLevel"/> is 0 (maximal) and
        /// <see cref="Collation"/> is "Latin1_General_100_BIN2".
        /// </summary>
        /// <param name="dbName">The database name.</param>
        public SqlServerDatabaseOptions( string dbName )
        {
            DatabaseName = dbName;
            Collation = "Latin1_General_100_BIN2";
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="o">Read only options.</param>
        public SqlServerDatabaseOptions( ISqlServerDatabaseOptions o )
        {
            if( o == null ) throw new ArgumentNullException( nameof( o ) );
            DatabaseName = o.DatabaseName;
            Collation = o.Collation;
            CompatibilityLevel = o.CompatibilityLevel;
        }

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the database collation.
        /// Defaults to <c>"Latin1_General_100_BIN2"</c>.
        /// The value is sent as-is in the "create database ... collate" statement: it must be a valid SQL Server collation name.
        /// </summary>
        public string Collation { get; set; }

        /// <summary>
        /// Gets or sets the compatibility level.
        /// Defaults to 0: the maximal compatibility level is used. 
        /// </summary>
        public int CompatibilityLevel { get; set; }

        /// <summary>
        /// Overridden to return the Database name, collation and compatibility level.
        /// </summary>
        /// <returns>A readable string.</returns>
        public override string ToString()
        {
            if( CompatibilityLevel != 0 )
            {
                return $"Database:{DatabaseName}, Collation:{Collation}, CompatibilityLevel:{CompatibilityLevel}";
            }
            return $"Database:{DatabaseName}, Collation:{Collation}";
        }
    }
}
