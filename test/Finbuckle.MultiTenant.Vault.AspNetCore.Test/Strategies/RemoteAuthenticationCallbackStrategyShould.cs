// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more information.

using Finbuckle.MultiTenant.Vault.AspNetCore.Strategies;
using Xunit;

namespace Finbuckle.MultiTenant.Vault.AspNetCore.Test.Strategies;

public class RemoteAuthenticationCallbackStrategyShould
{
    [Fact]
    public void HavePriorityNeg900()
    {
        var strategy = new RemoteAuthenticationCallbackStrategy(null!);
        Assert.Equal(-900, strategy.Priority);
    }

    [Fact]
    public async Task ReturnNullIfContextIsNotHttpContext()
    {
        var context = new object();
        var strategy = new RemoteAuthenticationCallbackStrategy(null!);

        Assert.Null(await strategy.GetIdentifierAsync(context));
    }
}
