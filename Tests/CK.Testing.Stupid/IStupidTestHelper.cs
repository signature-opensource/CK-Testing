using CK.Testing.Stupid;

namespace CK.Testing;

/// <summary>
/// StupidTestHelper is here to show the mixin implementation.
/// This IStupidTestHelper is the final, automatically implemented facade.
/// It doesn't define anything: it combines all its interfaces' implementation.
/// What this StupidTestHelper brings is defined in <see cref="IStupidTestHelperCore"/> and the
/// implementation is in <see cref="StupidTestHelper"/>.
/// <para>
/// It is based on <see cref="IMonitorTestHelper"/>: the SQL Server helpers are extension members of
/// <see cref="IMonitorTestHelper"/> (see <see cref="SqlServerTestHelperExtensions"/>), so this facade
/// exposes them too.
/// </para>
/// </summary>
public interface IStupidTestHelper : IMixinTestHelper, IMonitorTestHelper, IStupidTestHelperCore
{
}
