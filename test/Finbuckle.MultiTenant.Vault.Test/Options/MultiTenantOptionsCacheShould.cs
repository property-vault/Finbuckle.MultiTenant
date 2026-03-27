// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more information.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Finbuckle.MultiTenant.Vault.Abstractions;
using Finbuckle.MultiTenant.Options;
using Microsoft.Extensions.Options;
using Xunit;

namespace Finbuckle.MultiTenant.Test.Options;

public class MultiTenantOptionsCacheShould
{
    private readonly Guid _testid123 = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f39136e");
    private readonly Guid _diffid = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f391366");
    private readonly Guid _diff = Guid.Parse("c8d1f3c7-440e-4e76-bc77-12e33f391364");
    public class TestOptions
    {
        [Required]
        public string? DefaultConnectionString { get; set; }
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("name")]
    public void AddNamedOptionsForCurrentTenantOnlyOnAdd(string? name)
    {
        var ti = new TenantInfo { Id = _testid123, Identifier = "" };
        var tc = new MultiTenantContext<TenantInfo>(ti);
        var tca = new AsyncLocalMultiTenantContextAccessor<TenantInfo>();
        var tcs = (IMultiTenantContextSetter)tca;
        tcs.MultiTenantContext = tc;
        var cache = new MultiTenantOptionsCache<TestOptions>(tca);

        var options = new TestOptions();

        // Add new options.
        var result = cache.TryAdd(name, options);
        Assert.True(result);

        // Fail adding options under same name.
        result = cache.TryAdd(name, options);
        Assert.False(result);

        // Change the tenant id and confirm options can be added again.
        tcs.MultiTenantContext = new MultiTenantContext<TenantInfo>(new TenantInfo { Id = _diff, Identifier = "" });
        result = cache.TryAdd(name, options);
        Assert.True(result);
    }

    [Fact]
    public void HandleNullMultiTenantContextOnAdd()
    {
        var tca = new AsyncLocalMultiTenantContextAccessor<TenantInfo>();
        var cache = new MultiTenantOptionsCache<TestOptions>(tca);

        var options = new TestOptions();

        // Add new options, ensure no exception caused by null MultiTenantContext.
        var result = cache.TryAdd("", options);
        Assert.True(result);
    }

    [Fact]
    public void HandleNullMultiTenantContextOnGetOrAdd()
    {
        var tca = new AsyncLocalMultiTenantContextAccessor<TenantInfo>();
        var cache = new MultiTenantOptionsCache<TestOptions>(tca);

        var options = new TestOptions();

        // Add new options, ensure no exception caused by null MultiTenantContext.
        var result = cache.GetOrAdd("", () => options);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("name")]
    public void GetOrAddNamedOptionForCurrentTenantOnly(string? name)
    {
        var ti = new TenantInfo { Id = _testid123, Identifier = "" };
        var tc = new MultiTenantContext<TenantInfo>(ti);
        var tca = new AsyncLocalMultiTenantContextAccessor<TenantInfo>();
        var tcs = (IMultiTenantContextSetter)tca;
        tcs.MultiTenantContext = tc;
        var cache = new MultiTenantOptionsCache<TestOptions>(tca);

        var options = new TestOptions();
        var options2 = new TestOptions();

        // Add new options.
        var result = cache.GetOrAdd(name, () => options);
        Assert.Same(options, result);

        // Get the existing options if exists.
        result = cache.GetOrAdd(name, () => options2);
        Assert.NotSame(options2, result);

        // Confirm different tenant on same object is an add (ie it didn't exist there).
        ti = new TenantInfo { Id = _diffid, Identifier = ti.Identifier };
        tcs.MultiTenantContext = new MultiTenantContext<TenantInfo>(ti);
        result = cache.GetOrAdd(name, () => options2);
        Assert.Same(options2, result);
    }

    [Fact]
    public void ThrowsIfGetOrAddFactoryIsNull()
    {
        var tca = new AsyncLocalMultiTenantContextAccessor<TenantInfo>();
        var cache = new MultiTenantOptionsCache<TestOptions>(tca);

        Assert.Throws<ArgumentNullException>(() => cache.GetOrAdd("", null!));
    }

    [Fact]
    public void ThrowIfConstructorParamIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new MultiTenantOptionsCache<TestOptions>(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("name")]
    public void RemoveNamedOptionsForCurrentTenantOnly(string? name)
    {
        var ti = new TenantInfo { Id = _testid123, Identifier = "" };
        var tc = new MultiTenantContext<TenantInfo>(ti);
        var tca = new AsyncLocalMultiTenantContextAccessor<TenantInfo>();
        var tcs = (IMultiTenantContextSetter)tca;
        tcs.MultiTenantContext = tc;
        var cache = new MultiTenantOptionsCache<TestOptions>(tca);

        var options = new TestOptions();

        // Add new options.
        var result = cache.TryAdd(name, options);
        Assert.True(result);

        // Add under a different tenant.
        ti = new TenantInfo { Id = _diffid, Identifier = ti.Identifier };
        tcs.MultiTenantContext = new MultiTenantContext<TenantInfo>(ti);
        result = cache.TryAdd(name, options);
        Assert.True(result);
        result = cache.TryAdd("diffName", options);
        Assert.True(result);

        // Remove named options for current tenant.
        result = cache.TryRemove(name);
        Assert.True(result);
        var tenantCache = (ConcurrentDictionary<Guid, IOptionsMonitorCache<TestOptions>>?)cache.GetType()
            .GetField("map", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(cache);

        dynamic? tenantInternalCache = tenantCache?[ti.Id].GetType()
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tenantCache[ti.Id]);

        // Assert named options removed and other options on tenant left as-is.
        Assert.False(tenantInternalCache!.Keys.Contains(name));
        Assert.True(tenantInternalCache.Keys.Contains("diffName"));

        // Assert other tenant not affected.
        ti = new TenantInfo { Id = _testid123, Identifier = ti.Identifier };
        tcs.MultiTenantContext = new MultiTenantContext<TenantInfo>(ti);
        tenantInternalCache = tenantCache?[ti.Id].GetType()
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tenantCache[ti.Id]);
        Assert.True(tenantInternalCache!.ContainsKey(name ?? Microsoft.Extensions.Options.Options.DefaultName));
    }

    [Fact]
    public void ClearOptionsForCurrentTenantOnly()
    {
        var ti = new TenantInfo { Id = _testid123, Identifier = "" };
        var tc = new MultiTenantContext<TenantInfo>(ti);
        var tca = new AsyncLocalMultiTenantContextAccessor<TenantInfo>();
        var tcs = (IMultiTenantContextSetter)tca;
        tcs.MultiTenantContext = tc;
        var cache = new MultiTenantOptionsCache<TestOptions>(tca);

        var options = new TestOptions();

        // Add new options.
        var result = cache.TryAdd("", options);
        Assert.True(result);

        // Add under a different tenant.
        ti = new TenantInfo { Id = _diffid, Identifier = ti.Identifier };
        tcs.MultiTenantContext = new MultiTenantContext<TenantInfo>(ti);
        result = cache.TryAdd("", options);
        Assert.True(result);

        // Clear options on first tenant.
        ti = new TenantInfo { Id = _testid123, Identifier = ti.Identifier };
        tcs.MultiTenantContext = new MultiTenantContext<TenantInfo>(ti);
        cache.Clear();

        // Assert options cleared on this tenant.
        var tenantCache = (ConcurrentDictionary<Guid, IOptionsMonitorCache<TestOptions>>?)cache.GetType()
            .GetField("map", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(cache);

        dynamic? tenantInternalCache = tenantCache?[ti.Id].GetType()
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tenantCache[ti.Id]);
        Assert.True(tenantInternalCache!.IsEmpty);

        // Assert options still exist on other tenant.
        ti = new TenantInfo { Id = _diffid, Identifier = ti.Identifier };
        tcs.MultiTenantContext = new MultiTenantContext<TenantInfo>(ti);
        tenantInternalCache = tenantCache?[ti.Id].GetType()
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tenantCache[ti.Id]);
        Assert.False(tenantInternalCache!.IsEmpty);
    }

    [Fact]
    public void ClearOptionsForVaultIdOnly()
    {
        var ti = new TenantInfo { Id = _testid123, Identifier = "" };
        var tc = new MultiTenantContext<TenantInfo>(ti);
        var tca = new AsyncLocalMultiTenantContextAccessor<TenantInfo>();
        var tcs = (IMultiTenantContextSetter)tca;
        tcs.MultiTenantContext = tc;
        var cache = new MultiTenantOptionsCache<TestOptions>(tca);

        var options = new TestOptions();

        // Add new options.
        var result = cache.TryAdd("", options);
        Assert.True(result);

        // Add under a different tenant.
        ti = new TenantInfo { Id = _diffid, Identifier = ti.Identifier };
        tcs.MultiTenantContext = new MultiTenantContext<TenantInfo>(ti);
        result = cache.TryAdd("", options);
        Assert.True(result);

        // Clear options on first tenant.
        cache.Clear(_testid123);

        // Assert options cleared on this tenant.
        var tenantCache = (ConcurrentDictionary<Guid, IOptionsMonitorCache<TestOptions>>?)cache.GetType()
            .GetField("map", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(cache);

        dynamic? tenantInternalCache = tenantCache?[ti.Id].GetType()
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tenantCache[_testid123]);
        Assert.True(tenantInternalCache!.IsEmpty);

        // Assert options still exist on other tenant.
        tenantInternalCache = tenantCache?[_diffid].GetType()
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tenantCache[_diffid]);
        Assert.False(tenantInternalCache!.IsEmpty);
    }

    [Fact]
    public void ClearAllOptionsForClearAll()
    {
        var ti = new TenantInfo { Id = _testid123, Identifier = "" };
        var tc = new MultiTenantContext<TenantInfo>(ti);
        var tca = new AsyncLocalMultiTenantContextAccessor<TenantInfo>();
        var tcs = (IMultiTenantContextSetter)tca;
        tcs.MultiTenantContext = tc;
        var cache = new MultiTenantOptionsCache<TestOptions>(tca);

        var options = new TestOptions();

        // Add new options.
        var result = cache.TryAdd("", options);
        Assert.True(result);

        // Add under a different tenant.
        ti = new TenantInfo { Id = _diffid, Identifier = ti.Identifier };
        tcs.MultiTenantContext = new MultiTenantContext<TenantInfo>(ti);
        result = cache.TryAdd("", options);
        Assert.True(result);

        // Clear all options.
        cache.ClearAll();

        // Assert options cleared on this tenant.
        var tenantCache = (ConcurrentDictionary<Guid, IOptionsMonitorCache<TestOptions>>?)cache.GetType()
            .GetField("map", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(cache);

        ti = new TenantInfo { Id = _testid123, Identifier = ti.Identifier };
        dynamic? tenantInternalCache = tenantCache?[ti.Id].GetType()
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tenantCache[ti.Id]);
        Assert.True(tenantInternalCache!.IsEmpty);

        // Assert options cleared on other tenant.
        ti = new TenantInfo { Id = _diffid, Identifier = ti.Identifier };
        tcs.MultiTenantContext = new MultiTenantContext<TenantInfo>(ti);
        tenantInternalCache = tenantCache?[ti.Id].GetType()
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tenantCache[ti.Id]);
        Assert.True(tenantInternalCache!.IsEmpty);
    }
}
