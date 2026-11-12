using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

public sealed class HeaderRuleTests
{
    [Fact]
    public void Create_ForwardAuthorization_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            HeaderRule.Create("Authorization", HeaderRuleMode.Forward));
    }

    [Fact]
    public void Create_InjectValueWithCrlf_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            HeaderRule.Create("X-Trace", HeaderRuleMode.Inject, injectedValue: "ok\r\nX-Injected: evil"));
    }
}
