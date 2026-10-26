using MiniPos.Domain.Common;

namespace MiniPos.Domain.Sales;

/// <summary>Pure helpers for resolving and validating discounts against configured limits.</summary>
public static class DiscountResolver
{
    /// <summary>
    /// Resolves a discount request to a halala amount, applying limits and clamping.
    /// The <paramref name="valueHalalas"/> is already the resolved discount in halalas (not a percentage).
    /// Returns the resolved (possibly clamped) discount in halalas, or an error if it exceeds both limits.
    /// </summary>
    public static Result<long> Resolve(
        long originalAmountHalalas,
        long valueHalalas,
        int? maxPercent,
        long? maxFixedHalalas)
    {
        if (originalAmountHalalas < 0)
            return new Error("discount.invalid_original", "Original amount must be non-negative.", ErrorKind.Validation);

        if (valueHalalas <= 0)
            return new Error("discount.value_must_be_positive", "Discount value must be positive.", ErrorKind.Validation);

        // Clamp to the original amount (net cannot go below zero)
        long clamped = Math.Min(valueHalalas, originalAmountHalalas);

        if (maxPercent.HasValue && maxFixedHalalas.HasValue)
        {
            // Both limits set: satisfy EITHER — reject only if exceeds BOTH
            bool exceedsPercent = clamped > MaxFixedFromPercent(originalAmountHalalas, maxPercent.Value);
            bool exceedsFixed = clamped > maxFixedHalalas.Value;

            if (exceedsPercent && exceedsFixed)
                return new Error("discount.exceeds_limits",
                    $"Discount of {clamped} halalas exceeds both configured limits.", ErrorKind.Validation);

            // No clamping needed — either limit is satisfied
            return clamped;
        }

        if (maxPercent.HasValue)
        {
            // Only max % set: enforce as cap
            var maxFixed = MaxFixedFromPercent(originalAmountHalalas, maxPercent.Value);
            return Math.Min(clamped, maxFixed);
        }

        if (maxFixedHalalas.HasValue)
        {
            // Only max fixed set: enforce as cap
            return Math.Min(clamped, maxFixedHalalas.Value);
        }

        // No limits configured — no cap
        return clamped;
    }

    /// <summary>Converts a percentage limit to its equivalent fixed halala amount.</summary>
    private static long MaxFixedFromPercent(long originalAmountHalalas, int maxPercent)
    {
        // Half-up rounding: (original * percent + 50) / 100
        return (originalAmountHalalas * maxPercent + 50) / 100;
    }

    /// <summary>Converts a percentage value to a halala discount amount using half-up rounding.</summary>
    public static long PercentToHalalas(long originalAmountHalalas, int percent)
    {
        // Half-up rounding: (original * percent + 50) / 100
        return (originalAmountHalalas * percent + 50) / 100;
    }
}
