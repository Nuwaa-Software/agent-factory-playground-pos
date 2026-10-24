using MiniPos.Domain.Common;

namespace MiniPos.Domain.Catalog;

public sealed class Category : ITenantScoped
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CompanyId { get; init; }
    public required string NameEn { get; set; }
    public required string NameAr { get; set; }
    public string? Description { get; set; }
}