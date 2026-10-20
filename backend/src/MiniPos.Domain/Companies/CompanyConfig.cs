using MiniPos.Domain.Common;

namespace MiniPos.Domain.Companies;

public sealed class CompanyConfig : ITenantScoped
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CompanyId { get; init; }
    public required string Key { get; set; }
    public required string Value { get; set; }
}