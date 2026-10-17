namespace MiniPos.Domain.Common;

/// <summary>
/// Marks data that belongs to exactly one company (tenant). Every <c>ITenantScoped</c> entity gets a
/// global <c>CompanyId</c> query filter in the DbContext, so a request can only ever see its own
/// company's rows. Never bypass it with <c>IgnoreQueryFilters()</c> on a tenant endpoint.
/// </summary>
public interface ITenantScoped
{
    Guid CompanyId { get; }
}
