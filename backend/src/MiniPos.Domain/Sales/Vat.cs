namespace MiniPos.Domain.Sales;

/// <summary>Saudi VAT. Computed once on the invoice net total, rounded to the halala.</summary>
public static class Vat
{
    public const int RatePercent = 15;

    /// <summary>VAT on a net amount, in halalas. Rule: round half up (0.5 halala rounds up).</summary>
    public static long Compute(long netHalalas) => Compute(netHalalas, RatePercent);

    /// <summary>VAT on a net amount using the given rate (percent), in halalas. Rule: round half up.</summary>
    public static long Compute(long netHalalas, decimal ratePercent)
    {
        if (netHalalas < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(netHalalas), "Net amount cannot be negative.");
        }

        return (long)Math.Round(netHalalas * ratePercent / 100m, MidpointRounding.AwayFromZero);
    }
}
