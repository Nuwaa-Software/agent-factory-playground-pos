namespace MiniPos.Infrastructure.Tenancy;

/// <summary>The company (tenant) of the current request. Resolved from the authenticated user.</summary>
public interface ITenantContext
{
    Guid CompanyId { get; }
}

/// <summary>Request-scoped holder; the API sets it from the user's <c>company_id</c> claim.</summary>
public sealed class TenantContext : ITenantContext
{
    public Guid CompanyId { get; set; }
}
