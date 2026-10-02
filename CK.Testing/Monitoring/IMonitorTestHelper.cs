namespace CK.Testing;

/// <summary>
/// Mixin of <see cref="Monitoring.IMonitorTestHelperCore"/> and <see cref="IBasicTestHelper"/>.
/// </summary>
public interface IMonitorTestHelper : IMixinTestHelper, IBasicTestHelper, Monitoring.IMonitorTestHelperCore
{
}
