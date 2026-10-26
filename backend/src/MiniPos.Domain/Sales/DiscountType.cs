namespace MiniPos.Domain.Sales;

/// <summary>Type of discount to apply.</summary>
public enum DiscountType
{
    /// <summary>Percentage-based discount (0–100).</summary>
    Percent,

    /// <summary>Fixed amount in halalas.</summary>
    Fixed,
}
