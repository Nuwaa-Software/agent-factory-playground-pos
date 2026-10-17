using MiniPos.Domain.Common;

namespace MiniPos.Domain.Sales;

public sealed class Sale : ITenantScoped
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid CompanyId { get; init; }

    public int Number { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public List<SaleLine> Lines { get; init; } = [];

    /// <summary>Sum of line net amounts, VAT excluded.</summary>
    public long NetHalalas { get; set; }

    public long VatHalalas { get; set; }

    public long TotalHalalas { get; set; }
}

public sealed class SaleLine
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid ProductId { get; init; }

    public required string ProductNameEn { get; init; }

    public required string ProductNameAr { get; init; }

    public int Quantity { get; init; }

    public long UnitPriceHalalas { get; init; }

    public long LineNetHalalas => UnitPriceHalalas * Quantity;
}
