using System;

namespace CK.Testing.Stupid;

/// <summary>
/// StupidTestHelper is here to show the mixin implementation.
/// This IStupidTestHelperCore defines the actual (stupid) things that is added to the TestHelper.
/// The actual implementation is in <see cref="StupidTestHelper"/>.
/// </summary>
public interface IStupidTestHelperCore
{
    /// <summary>
    /// Gets the name of the last database that the SQL Server helpers created, reset or dropped.
    /// This helper subscribes to the static <see cref="SqlServerTestHelperExtensions.OnDatabaseCreatedOrDropped"/> event.
    /// When this event fires, it captures the database name and calls <see cref="StupidMethod"/>.
    /// </summary>
    string? LastDatabaseCreatedOrDroppedName { get; }

    /// <summary>
    /// Gets the total number of calls to <see cref="StupidMethod"/>.
    /// </summary>
    int CountOfStupidMethodCalls { get; }

    /// <summary>
    /// Does nothing more than incrementing <see cref="CountOfStupidMethodCalls"/> and raising <see cref="OnStupidMethodCalled"/>.
    /// </summary>
    void StupidMethod();

    /// <summary>
    /// Fires on each call to <see cref="StupidMethod"/>.
    /// </summary>
    event EventHandler OnStupidMethodCalled;
}
