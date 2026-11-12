using Xunit;

namespace EnterpriseSecureProxy.Tests.Support;

/// <summary>
/// Groups all tests that boot the real host via <see cref="ProxyWebApplicationFactory"/> into a single
/// non-parallel collection, since that factory mutates process environment variables.
/// </summary>
[CollectionDefinition("WebFactory", DisableParallelization = true)]
public sealed class WebFactoryCollection
{
}
