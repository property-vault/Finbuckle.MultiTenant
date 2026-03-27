// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more information.

using System.Text;
using System.Text.Json;
using Finbuckle.MultiTenant.Vault.Abstractions;
using Finbuckle.MultiTenant.Stores;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Finbuckle.MultiTenant.Test.Stores;

public class DistributedCacheStoreShould : MultiTenantStoreTestBase
{
    private readonly Guid _lolid = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f391364");
    private readonly Guid _testid = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f391366");
    [Fact]
    public async Task ThrowOnGetAllTenantsFromStoreAsync()
    {
        var store = await CreateTestStore();
        await Assert.ThrowsAsync<NotImplementedException>(async () => await store.GetAllAsync());
    }


    [Fact]
    public async Task RemoveReturnsFalseWhenNoMatchingIdentifierFound()
    {
        var store = await CreateTestStore();

        var r = await store.RemoveAsync("DoesNotExist");

        Assert.False(r);
    }

    [Fact]
    public async Task AddDualEntriesOnAdd()
    {
        var store = await CreateTestStore();

        var t2 = await store.GetByIdentifierAsync("lol");
        var t1 = await store.GetAsync(_lolid);

        Assert.NotNull(t1);
        Assert.NotNull(t2);
        Assert.Equal(_lolid, t1.Id);
        Assert.Equal(_lolid, t2.Id);
        Assert.Equal("lol", t1.Identifier);
        Assert.Equal("lol", t2.Identifier);
    }

    [Fact]
    public async Task RefreshDualEntriesOnTryGet()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TenantInfo
            {
                Id = _lolid,
                Identifier = "lol"
            })));

        var store = new DistributedCacheStore<TenantInfo>(cache.Object, Constants.TenantToken, TimeSpan.FromSeconds(1));

        await store.GetAsync(_lolid);
        cache.Verify(c => c.RefreshAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RefreshDualEntriesOnTryGetByIdentifier()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TenantInfo
                { Id = _lolid, Identifier = "lol" })));

        var store = new DistributedCacheStore<TenantInfo>(cache.Object, Constants.TenantToken, TimeSpan.FromSeconds(1));

        await store.GetByIdentifierAsync("lol-id");
        cache.Verify(c => c.RefreshAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SetSlidingExpirationOnAdd()
    {
        var cache = new Mock<IDistributedCache>();
        var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(1) };

        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((_, _, opts, _) =>
            {
                Assert.Equal(options.SlidingExpiration, opts.SlidingExpiration);
            })
            .Returns(Task.CompletedTask);

        var store = new DistributedCacheStore<TenantInfo>(cache.Object, Constants.TenantToken, TimeSpan.FromSeconds(1));

        await store.AddAsync(new TenantInfo { Id = _testid, Identifier = "test" });
    }

    [Fact]
    public async Task SetSlidingExpirationOnUpdate()
    {
        var cache = new Mock<IDistributedCache>();
        var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(1) };

        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((_, _, opts, _) =>
            {
                Assert.Equal(options.SlidingExpiration, opts.SlidingExpiration);
            })
            .Returns(Task.CompletedTask);

        var store = new DistributedCacheStore<TenantInfo>(cache.Object, Constants.TenantToken, TimeSpan.FromSeconds(1));

        await store.AddAsync(new TenantInfo { Id = _testid, Identifier = "test" });
    }

    // Basic store functionality tested in MultiTenantStoresShould.cs

    protected override async Task<IMultiTenantStore<TenantInfo>> CreateTestStore()
    {
        var services = new ServiceCollection();
        services.AddOptions().AddDistributedMemoryCache();
        var sp = services.BuildServiceProvider();

        var store = new DistributedCacheStore<TenantInfo>(sp.GetRequiredService<IDistributedCache>(),
            Constants.TenantToken, TimeSpan.MaxValue);

        return await PopulateTestStore(store);
    }

    [Fact]
    public override async Task GetTenantInfoFromStoreById()
    {
        await base.GetTenantInfoFromStoreById();
    }

    [Fact]
    public override async Task ReturnNullWhenGettingByIdIfTenantInfoNotFound()
    {
        await base.ReturnNullWhenGettingByIdIfTenantInfoNotFound();
    }

    [Fact]
    public override async Task GetTenantInfoFromStoreByIdentifier()
    {
        await base.GetTenantInfoFromStoreByIdentifier();
    }

    [Fact]
    public override async Task ReturnNullWhenGettingByIdentifierIfTenantInfoNotFound()
    {
        await base.ReturnNullWhenGettingByIdentifierIfTenantInfoNotFound();
    }

    [Fact]
    public override async Task AddTenantInfoToStore()
    {
        await base.AddTenantInfoToStore();
    }

    [Fact]
    public override async Task RemoveTenantInfoFromStore()
    {
        await base.RemoveTenantInfoFromStore();
    }

    [Fact]
    public override async Task UpdateTenantInfoInStore()
    {
        await base.UpdateTenantInfoInStore();
    }
}
