// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more information.

using Finbuckle.MultiTenant.Vault.Abstractions;
using Finbuckle.MultiTenant.Vault.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Finbuckle.MultiTenant.Vault.EntityFrameworkCore.Test.Extensions.EntityTypeBuilderExtensions;

public class TestDbContext(Action<ModelBuilder>? config, TenantInfo tenantInfo, DbContextOptions options)
    : EntityFrameworkCore.MultiTenantDbContext(new StaticMultiTenantContextAccessor<TenantInfo>(tenantInfo), options)
{
    public DbSet<MyMultiTenantThing>? MyMultiTenantThings { get; set; }
    public DbSet<MyThingWithVaultId>? MyThingsWithVaultIds { get; set; }
    public DbSet<MyThingWithIntVaultId>? MyThingsWithIntVaultId { get; set; }
    public DbSet<MyMultiTenantThingWithAttribute>? MyMultiTenantThingsWithAttribute { get; set; }
    public DbSet<MyNonMultiTenantThing>? MyNonMultiTenantThings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // if the test passed in a custom builder use it
        if (config != null)
            config(modelBuilder);
        // or use the standard builder configuration
        else
        {
            modelBuilder.Entity<MyMultiTenantThing>().IsMultiTenant();
            modelBuilder.Entity<MyThingWithVaultId>().IsMultiTenant();
        }

        base.OnModelCreating(modelBuilder);
    }
}

public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context)
    {
        return new object();
    }

    public object Create(DbContext context, bool designTime)
    {
        return new object();
    }
}

public class MyMultiTenantThing
{
    public int Id { get; set; }
}

public class MyNonMultiTenantThing
{
    public int Id { get; set; }
}

[MultiTenant]
public class MyMultiTenantThingWithAttribute
{
    public Guid Id { get; set; }
}

public class MyThingWithVaultId
{
    public int Id { get; set; }
    public Guid VaultId { get; set; }
}

public class MyThingWithIntVaultId
{
    public int Id { get; set; }
    public int VaultId { get; set; }
}
