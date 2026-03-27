// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more information.

using Finbuckle.MultiTenant.Vault.Abstractions;
using Xunit;

#pragma warning disable xUnit1013 // Public method should be marked as test

namespace Finbuckle.MultiTenant.Test.Stores;

public abstract class MultiTenantStoreTestBase
{
    private readonly Guid _initechid = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f39136e");
    private readonly Guid _lolid = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f391364");
    private readonly Guid _fake123 = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f391366");
    private readonly Guid _id = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f391363");
    protected abstract Task<IMultiTenantStore<TenantInfo>> CreateTestStore();

    protected virtual async Task<IMultiTenantStore<TenantInfo>> PopulateTestStore(IMultiTenantStore<TenantInfo> store)
    {
        await store.AddAsync(new TenantInfo { Id = _initechid, Identifier = "initech", Name = "Initech" });
        await store.AddAsync(new TenantInfo { Id = _lolid, Identifier = "lol", Name = "Lol, Inc." });

        return store;
    }

    //[Fact]
    public virtual async Task GetTenantInfoFromStoreById()
    {
        var store = await CreateTestStore();

        Assert.Equal("initech", (await store.GetAsync(_initechid))!.Identifier);
    }

    //[Fact]
    public virtual async Task ReturnNullWhenGettingByIdIfTenantInfoNotFound()
    {
        var store = await CreateTestStore();

        Assert.Null(await store.GetAsync(_fake123));
    }

    //[Fact]
    public virtual async Task GetTenantInfoFromStoreByIdentifier()
    {
        var store = await CreateTestStore();

        Assert.Equal("initech", (await store.GetByIdentifierAsync("initech"))!.Identifier);
    }

    //[Fact]
    public virtual async Task ReturnNullWhenGettingByIdentifierIfTenantInfoNotFound()
    {
        var store = await CreateTestStore();
        Assert.Null(await store.GetByIdentifierAsync("fake123"));
    }

    //[Fact]
    public virtual async Task AddTenantInfoToStore()
    {
        var store = await CreateTestStore();

        Assert.Null(await store.GetByIdentifierAsync("identifier"));
        Assert.True(await store.AddAsync(new TenantInfo { Id = _id, Identifier = "identifier", Name = "name" }));
        Assert.NotNull(await store.GetByIdentifierAsync("identifier"));
    }

    //[Fact]
    public virtual async Task UpdateTenantInfoInStore()
    {
        var store = await CreateTestStore();

        var result = await store.UpdateAsync(new TenantInfo { Id = _initechid, Identifier = "initech2" });
        Assert.True(result);
    }

    //[Fact]
    public virtual async Task RemoveTenantInfoFromStore()
    {
        var store = await CreateTestStore();
        Assert.NotNull(await store.GetByIdentifierAsync("initech"));
        Assert.True(await store.RemoveAsync("initech"));
        Assert.Null(await store.GetByIdentifierAsync("initech"));
    }

    //[Fact]
    public virtual async Task GetAllTenantsFromStoreAsync()
    {
        var store = await CreateTestStore();
        Assert.Equal(2, (await store.GetAllAsync()).Count());
    }

    //[Fact]
    public virtual async Task GetAllTenantsFromStoreAsyncSkip1Take1()
    {
        var store = await CreateTestStore();
        var tenants = (await store.GetAllAsync(1, 1)).ToList();
        Assert.Single(tenants);

        var tenant = tenants.FirstOrDefault();
        Assert.NotNull(tenant);
        Assert.Equal("lol", tenant.Identifier);
    }
}
