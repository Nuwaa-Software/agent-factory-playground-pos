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

    /// <summary>Sale-level discount amount in halalas (0 = no discount).</summary>
    public long SaleDiscountHalalas { get; set; }

    /// <summary>Reason for the sale-level discount.</summary>
    public string? SaleDiscountReason { get; set; }

    public long VatHalalas { get; set; }

    public long TotalHalalas { get; set; }
    
    public bool Voided { get; set; }

    public DateTimeOffset? VoidedAt { get; set; }

    public PaymentType PaymentType { get; set; } = PaymentType.Cash;

    public Guid? CustomerId { get; set; }
}

public sealed class SaleLine
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid ProductId { get; init; }

    public required string ProductNameEn { get; init; }

    public required string ProductNameAr { get; init; }

    public int Quantity { get; init; }

    public long UnitPriceHalalas { get; init; }

    /// <summary>Discount amount in halalas (0 = no discount).</summary>
    public long DiscountHalalas { get; set; }

    /// <summary>Reason for the line-level discount.</summary>
    public string? DiscountReason { get; set; }

    public long LineNetHalalas => Math.Max(0, UnitPriceHalalas * Quantity - DiscountHalalas);
}