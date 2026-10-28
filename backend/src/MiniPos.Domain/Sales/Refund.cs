using MiniPos.Domain.Common;

namespace MiniPos.Domain.Sales;

public sealed class Refund : ITenantScoped
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid CompanyId { get; init; }

    public Guid SaleId { get; init; }

    /// <summary>Total refund amount in halalas (net + prorated VAT).</summary>
    public long TotalHalalas { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>User name from Claims.UserName, falling back to role name.</summary>
    public string RefundedBy { get; set; } = "";
}
