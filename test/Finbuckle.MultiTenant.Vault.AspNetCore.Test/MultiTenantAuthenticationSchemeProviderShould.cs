// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more information.

using Finbuckle.MultiTenant.Vault.Abstractions;
using Finbuckle.MultiTenant.Vault.AspNetCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Finbuckle.MultiTenant.Vault.AspNetCore.Test;

public class MultiTenantAuthenticationSchemeProviderShould
{
    private readonly Guid _tenant1 = Guid.Parse("27916938-4c49-4ca4-ad14-4bfdbaa52e6f");
    private readonly Guid _tenant2 = Guid.Parse("27916938-4c49-4ca4-ad14-4bfdbaa52e66");
    
    [Fact]
    public async Task ReturnPerTenantAuthenticationOptions()
    {
        var services = new ServiceCollection();
        services.AddAuthentication()
            .AddCookie("tenant1Scheme")
            .AddCookie("tenant2Scheme");

        services.AddMultiTenant<TenantInfo>()
            .WithPerTenantAuthentication();

        services.ConfigureAllPerTenant<AuthenticationOptions, TenantInfo>((ao, ti) =>
        {
            ao.DefaultChallengeScheme = ti.Identifier + "Scheme";
        });

        var sp = services.BuildServiceProvider();

        var tenant1 = new TenantInfo { Id = _tenant1, Identifier = "tenant1" };

        var tenant2 = new TenantInfo { Id = _tenant2, Identifier = "tenant2" };

        var mtc = new MultiTenantContext<TenantInfo>(tenant1);
        var setter = sp.GetRequiredService<IMultiTenantContextSetter>();
        setter.MultiTenantContext = mtc;

        var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();

        var option = await schemeProvider.GetDefaultChallengeSchemeAsync();

        Assert.NotNull(option);
        Assert.Equal("tenant1Scheme", option.Name);

        mtc = new MultiTenantContext<TenantInfo>(tenant2);
        setter.MultiTenantContext = mtc;
        option = await schemeProvider.GetDefaultChallengeSchemeAsync();

        Assert.NotNull(option);
        Assert.Equal("tenant2Scheme", option.Name);
    }
}
