// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more information.

using Finbuckle.MultiTenant.Vault.Abstractions;
using Finbuckle.MultiTenant.Vault.EntityFrameworkCore.Stores;
using Microsoft.EntityFrameworkCore;

namespace Finbuckle.MultiTenant.Vault.EntityFrameworkCore.Test.Extensions.MultiTenantBuilderExtensions;

public class TestEfCoreStoreDbContext : EFCoreStoreDbContext<TenantInfo>
{
    public TestEfCoreStoreDbContext(DbContextOptions options) : base(options)
    {
    }
}
