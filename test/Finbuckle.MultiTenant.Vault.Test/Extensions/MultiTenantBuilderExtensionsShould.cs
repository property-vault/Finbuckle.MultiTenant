// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more information.

using Finbuckle.MultiTenant.Vault.Abstractions;
using Finbuckle.MultiTenant.Extensions;
using Finbuckle.MultiTenant.Stores;
using Finbuckle.MultiTenant.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Finbuckle.MultiTenant.Test.Extensions;

public class MultiTenantBuilderExtensionsShould
{
    private readonly Guid _initechid = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f39136e");
    private readonly Guid _lolid = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f391364");
    
    private readonly Guid _initech = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f391365");
    private readonly Guid _lol = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f391362");
    
    [Fact]
    public void AddDistributedCacheStoreDefault()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        builder.WithDistributedCacheStore();
        var sp = services.BuildServiceProvider();
        var store = sp.GetRequiredService<IMultiTenantStore<TenantInfo>>();
        Assert.IsType<DistributedCacheStore<TenantInfo>>(store);
    }

    [Fact]
    public void AddDistributedCacheStoreWithSlidingExpiration()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        builder.WithDistributedCacheStore(TimeSpan.FromMinutes(5));
        var sp = services.BuildServiceProvider();
        var store = sp.GetRequiredService<IMultiTenantStore<TenantInfo>>();
        Assert.IsType<DistributedCacheStore<TenantInfo>>(store);
    }

    [Fact]
    public void AddHttpRemoteStoreAndHttpRemoteStoreClient()
    {
        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        builder.WithHttpRemoteStore("https://example.com");
        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<HttpRemoteStoreClient<TenantInfo>>();
        var store = sp.GetRequiredService<IMultiTenantStore<TenantInfo>>();
        Assert.IsType<HttpRemoteStore<TenantInfo>>(store);
    }

    [Fact]
    public void AddHttpRemoteStoreWithHttpClientBuilders()
    {
        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        var flag = false;
        builder.WithHttpRemoteStore("https://example.com", _ => flag = true);
        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<HttpRemoteStoreClient<TenantInfo>>();
        var store = sp.GetRequiredService<IMultiTenantStore<TenantInfo>>();
        Assert.IsType<HttpRemoteStore<TenantInfo>>(store);
        Assert.True(flag);
    }

    [Fact]
    public async Task AddConfigurationStoreWithDefaults()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("ConfigurationStoreTestSettings.json");
        var configuration = configBuilder.Build();

        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        builder.WithConfigurationStore();
        services.AddSingleton<IConfiguration>(configuration);
        var sp = services.BuildServiceProvider();

        var store = sp.GetRequiredService<IMultiTenantStore<TenantInfo>>();
        Assert.IsType<ConfigurationStore<TenantInfo>>(store);

        var tc = await store.GetByIdentifierAsync("initech");
        Assert.Equal(_initechid, tc!.Id);
        Assert.Equal("initech", tc.Identifier);

        tc = await store.GetByIdentifierAsync("lol");
        Assert.Equal(_lolid, tc!.Id);
        Assert.Equal("lol", tc.Identifier);
    }

    [Fact]
    public async Task AddConfigurationStoreWithSectionName()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("ConfigurationStoreTestSettings.json");
        IConfiguration configuration = configBuilder.Build();

        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);

        // Non-default section name.
        configuration = configuration.GetSection("Finbuckle");
        builder.WithConfigurationStore(configuration, "MultiTenant:Stores:ConfigurationStore");
        var sp = services.BuildServiceProvider();

        var store = sp.GetRequiredService<IMultiTenantStore<TenantInfo>>();
        Assert.IsType<ConfigurationStore<TenantInfo>>(store);

        var tc = await store.GetByIdentifierAsync("initech");
        Assert.Equal(_initechid, tc!.Id);
        Assert.Equal("initech", tc.Identifier);

        tc = await store.GetByIdentifierAsync("lol");
        Assert.Equal(_lolid, tc!.Id);
        Assert.Equal("lol", tc.Identifier);
    }

    [Fact]
    public void ThrowIfNullParamAddingInMemoryStore()
    {
        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        Assert.Throws<ArgumentNullException>(()
            => builder.WithInMemoryStore(null!));
    }

    [Fact]
    public async Task AddInMemoryStoreWithCaseSensitivity()
    {
        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        builder.WithInMemoryStore(options =>
        {
            options.IsCaseSensitive = true;
            options.Tenants.Add(new TenantInfo { Id = _lol, Identifier = "lol" });
        });
        var sp = services.BuildServiceProvider();

        var store = sp.GetRequiredService<IMultiTenantStore<TenantInfo>>();
        Assert.IsType<InMemoryStore<TenantInfo>>(store);

        var tc = await store.GetByIdentifierAsync("lol");
        Assert.Equal(_lol, tc!.Id);
        Assert.Equal("lol", tc.Identifier);

        // Case sensitive test.
        tc = await store.GetByIdentifierAsync("LOL");
        Assert.Null(tc);
    }

    [Fact]
    public async Task AddEchoStore()
    {
        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        builder.WithEchoStore();
        var sp = services.BuildServiceProvider();

        var store = sp.GetRequiredService<IMultiTenantStore<TenantInfo>>();
        Assert.IsType<EchoStore<TenantInfo>>(store);

        var tc = await store.GetByIdentifierAsync("initech");
        Assert.Equal("initech", tc!.Identifier);

        tc = await store.GetByIdentifierAsync("lol");
        Assert.Equal("lol", tc!.Identifier);
    }

    [Fact]
    public void AddDelegateStrategy()
    {
        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        builder.WithDelegateStrategy(_ => Task.FromResult<string?>("Hi"));
        var sp = services.BuildServiceProvider();

        var strategy = sp.GetRequiredService<IMultiTenantStrategy>();
        Assert.IsType<DelegateStrategy>(strategy);
    }

    [Fact]
    public void AddTypedDelegateStrategy()
    {
        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        builder.WithDelegateStrategy<int, TenantInfo>(context => Task.FromResult(context.ToString())!);
        var sp = services.BuildServiceProvider();

        var strategy = sp.GetRequiredService<IMultiTenantStrategy>();
        Assert.IsType<DelegateStrategy>(strategy);
    }

    [Fact]
    public async Task ReturnNullForWrongTypeSendToTypedDelegateStrategy()
    {
        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        builder.WithDelegateStrategy<int, TenantInfo>(_ => Task.FromResult("Shouldn't ever get here")!);
        var sp = services.BuildServiceProvider();

        var strategy = sp.GetRequiredService<IMultiTenantStrategy>();
        var identifier = await strategy.GetIdentifierAsync(new object());
        Assert.Null(identifier);
    }

    private class BaseCtx
    {
    }

    private class DerivedCtx : BaseCtx
    {
    }

    [Fact]
    public async Task InvokeTypedDelegateWhenRuntimeContextIsDerivedType()
    {
        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        builder.WithDelegateStrategy<BaseCtx, TenantInfo>(ctx => Task.FromResult<string?>($"ok-{ctx.GetType().Name}"));
        var sp = services.BuildServiceProvider();

        var strategy = sp.GetRequiredService<IMultiTenantStrategy>();
        var identifier = await strategy.GetIdentifierAsync(new DerivedCtx());

        Assert.Equal("ok-DerivedCtx", identifier);
    }

    [Fact]
    public async Task ReturnNullWhenRuntimeContextIsBaseOfExpectedDerivedType()
    {
        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        builder.WithDelegateStrategy<DerivedCtx, TenantInfo>(ctx =>
            Task.FromResult<string?>($"ok-{ctx.GetType().Name}"));
        var sp = services.BuildServiceProvider();

        var strategy = sp.GetRequiredService<IMultiTenantStrategy>();
        var identifier = await strategy.GetIdentifierAsync(new BaseCtx());

        Assert.Null(identifier);
    }

    [Fact]
    public void AddStaticStrategy()
    {
        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        builder.WithStaticStrategy("initech");
        var sp = services.BuildServiceProvider();

        var strategy = sp.GetRequiredService<IMultiTenantStrategy>();
        Assert.IsType<StaticStrategy>(strategy);
    }

    [Fact]
    public void ThrowIfNullParamAddingStaticStrategy()
    {
        var services = new ServiceCollection();
        var builder = new MultiTenantBuilder<TenantInfo>(services);
        Assert.Throws<ArgumentNullException>(()
            => builder.WithStaticStrategy(null!));
    }
}
