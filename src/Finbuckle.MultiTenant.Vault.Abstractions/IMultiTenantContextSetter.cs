namespace Finbuckle.MultiTenant.Vault.Abstractions;

/// <summary>
/// Interface used to set the <see cref="IMultiTenantContext"/>. This is an implementation detail and not intended for general use.
/// </summary>
public interface IMultiTenantContextSetter
{
    /// <summary>
    /// Sets the current <see cref="IMultiTenantContext"/>.
    /// </summary>
    IMultiTenantContext MultiTenantContext { set; }
}
