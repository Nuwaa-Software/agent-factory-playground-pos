using MiniPos.Domain.Common;

namespace MiniPos.Domain.Customers;

public sealed class Customer : ITenantScoped
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid CompanyId { get; init; }

    public required string Name { get; set; }

    public string? Phone { get; set; }
}
