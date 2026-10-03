using System;
using System.Threading;

namespace CK.Testing;

/// <summary>
/// This sample shows how a package extends the <see cref="IMonitorTestHelper"/>: with extension members
/// and static state. There is no interface and no class to implement.
/// <para>
/// The state lives in static fields: there is only one <see cref="MonitorTestHelper.TestHelper"/>.
/// An event cannot be declared in an extension block, so <see cref="OnStupidMethodCalled"/> is a static event.
/// </para>
/// </summary>
public static class StupidTestHelperExtensions
{
    static string? _lastDatabaseCreatedOrDroppedName;
    static int _countCall;

    /// <summary>
    /// The static constructor runs before the first use of any member of this class.
    /// The subscription to the static <see cref="SqlServerTestHelperExtensions.OnDatabaseCreatedOrDropped"/>
    /// event lives as long as the process.
    /// </summary>
    static StupidTestHelperExtensions()
    {
        SqlServerTestHelperExtensions.OnDatabaseCreatedOrDropped += ( source, e ) =>
        {
            _lastDatabaseCreatedOrDroppedName = e.DatabaseOptions.DatabaseName;
            DoStupidMethod();
        };
    }

    /// <summary>
    /// Fires on each call to <c>StupidMethod</c>.
    /// </summary>
    public static event EventHandler? OnStupidMethodCalled;

    /// <param name="helper">This helper.</param>
    extension( IMonitorTestHelper helper )
    {
        /// <summary>
        /// Gets the name of the last database that the SQL Server helpers created, reset or dropped.
        /// This captures the database name when the static <see cref="SqlServerTestHelperExtensions.OnDatabaseCreatedOrDropped"/>
        /// event fires, and calls <c>StupidMethod</c>.
        /// </summary>
        public string? LastDatabaseCreatedOrDroppedName => _lastDatabaseCreatedOrDroppedName;

        /// <summary>
        /// Gets the total number of calls to <c>StupidMethod</c>.
        /// </summary>
        public int CountOfStupidMethodCalls => _countCall;

        /// <summary>
        /// Does nothing more than incrementing <c>CountOfStupidMethodCalls</c> and raising <see cref="OnStupidMethodCalled"/>.
        /// </summary>
        public void StupidMethod() => DoStupidMethod();
    }

    static void DoStupidMethod()
    {
        Interlocked.Increment( ref _countCall );
        OnStupidMethodCalled?.Invoke( null, EventArgs.Empty );
    }
}
