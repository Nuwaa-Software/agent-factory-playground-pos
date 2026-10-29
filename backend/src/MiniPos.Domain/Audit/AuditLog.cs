using MiniPos.Domain.Common;

namespace MiniPos.Domain.Audit;

public sealed class AuditLog : ITenantScoped
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid CompanyId { get; init; }

    /// <summary>Action name, e.g. "price_changed", "stock_adjusted", "sale_voided", "sale_refunded".</summary>
    public required string Action { get; set; }

    /// <summary>Entity type, e.g. "Product", "Sale".</summary>
    public required string EntityName { get; set; }

    /// <summary>Id of the entity that was changed.</summary>
    public Guid EntityId { get; init; }

    /// <summary>User name from Claims.UserName, falling back to role name.</summary>
    public required string ActorName { get; set; }

    /// <summary>JSON or free-text details about the change.</summary>
    public string Details { get; init; } = "";

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
