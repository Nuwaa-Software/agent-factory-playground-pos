using MiniPos.Domain.Sales;

namespace MiniPos.Tests.Unit;

public class DiscountResolverTests
{
    [Fact]
    public void Fixed_discount_within_limit_returns_value()
    {
        var result = DiscountResolver.Resolve(1000, 200, null, null);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(200);
    }

    [Fact]
    public void Percent_discount_within_limit_returns_halalas()
    {
        // First convert percent to halalas
        var halalas = DiscountResolver.PercentToHalalas(1000, 10);
        halalas.Should().Be(100); // (1000 * 10 + 50) / 100 = 100

        var result = DiscountResolver.Resolve(1000, halalas, null, null);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(100);
    }

    [Fact]
    public void Percent_to_halalas_half_up_rounds_correctly()
    {
        // 33% of 1000 = 330 exactly
        DiscountResolver.PercentToHalalas(1000, 33).Should().Be(330);

        // 33% of 999 = 329.67 → rounds to 330 (half-up)
        DiscountResolver.PercentToHalalas(999, 33).Should().Be(330);

        // 1% of 3 = 0.03 → rounds to 0 (half-up)
        DiscountResolver.PercentToHalalas(3, 1).Should().Be(0);

        // 1% of 7 = 0.07 → rounds to 0 (half-up)
        DiscountResolver.PercentToHalalas(7, 1).Should().Be(0);

        // 1% of 50 = 0.5 → rounds to 1 (half-up)
        DiscountResolver.PercentToHalalas(50, 1).Should().Be(1);

        // 15% of 7 = 1.05 → rounds to 1 (half-up)
        DiscountResolver.PercentToHalalas(7, 15).Should().Be(1);

        // 15% of 3 = 0.45 → rounds to 0 (half-up)
        DiscountResolver.PercentToHalalas(3, 15).Should().Be(0);
    }

    [Fact]
    public void Fixed_discount_clamped_to_zero_net()
    {
        var result = DiscountResolver.Resolve(100, 200, null, null);
        result.IsSuccess.Should().BeTrue();
        // Clamped to original amount (100), so net = 0
        result.Value.Should().Be(100);
    }

    [Fact]
    public void Percent_discount_clamped_to_zero_net()
    {
        var halalas = DiscountResolver.PercentToHalalas(100, 200);
        // 200% of 100 = 200, clamped to 100
        var result = DiscountResolver.Resolve(100, halalas, null, null);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(100);
    }

    [Fact]
    public void No_limits_allows_any_discount_up_to_total()
    {
        var halalas = DiscountResolver.PercentToHalalas(500, 100);
        halalas.Should().Be(500); // 100% of 500 = 500

        var result = DiscountResolver.Resolve(500, halalas, null, null);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(500);

        var result2 = DiscountResolver.Resolve(500, 500, null, null);
        result2.IsSuccess.Should().BeTrue();
        result2.Value.Should().Be(500);
    }

    [Fact]
    public void Max_percent_limit_enforced()
    {
        // Requested: 60% of 1000 = 600 halalas
        var requestedHalalas = DiscountResolver.PercentToHalalas(1000, 60);
        requestedHalalas.Should().Be(600);

        // Max is 50% → max fixed = (1000 * 50 + 50) / 100 = 500
        var result = DiscountResolver.Resolve(1000, requestedHalalas, maxPercent: 50, maxFixedHalalas: null);
        result.IsSuccess.Should().BeTrue();
        // Clamped to 500 (max % limit)
        result.Value.Should().Be(500);
    }

    [Fact]
    public void Max_fixed_limit_enforced()
    {
        var result = DiscountResolver.Resolve(1000, 200, maxPercent: null, maxFixedHalalas: 100);
        result.IsSuccess.Should().BeTrue();
        // Clamped to 100 (max fixed limit)
        result.Value.Should().Be(100);
    }

    [Fact]
    public void Both_limits_set_satisfy_either_rule()
    {
        // Exceeds max % but within max fixed → valid
        var r1Halalas = DiscountResolver.PercentToHalalas(1000, 60);
        r1Halalas.Should().Be(600);
        var r1 = DiscountResolver.Resolve(1000, r1Halalas, maxPercent: 50, maxFixedHalalas: 700);
        r1.IsSuccess.Should().BeTrue();
        // 600 > 50% (500) but within 700 → clamped to 600
        r1.Value.Should().Be(600);

        // Exceeds max fixed but within max % → valid
        var r2 = DiscountResolver.Resolve(1000, 600, maxPercent: 70, maxFixedHalalas: 500);
        r2.IsSuccess.Should().BeTrue();
        // 600 > 500 but within 70% (700) → clamped to 600
        r2.Value.Should().Be(600);
    }

    [Fact]
    public void Exceeding_both_limits_returns_error()
    {
        var halalas = DiscountResolver.PercentToHalalas(1000, 60);
        halalas.Should().Be(600);
        var result = DiscountResolver.Resolve(1000, halalas, maxPercent: 50, maxFixedHalalas: 500);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("discount.exceeds_limits");
    }

    [Fact]
    public void Negative_original_amount_returns_error()
    {
        var result = DiscountResolver.Resolve(-1, 100, null, null);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Zero_value_returns_error()
    {
        var result = DiscountResolver.Resolve(1000, 0, null, null);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Percent_to_halalas_exact()
    {
        DiscountResolver.PercentToHalalas(200, 50).Should().Be(100);
        DiscountResolver.PercentToHalalas(300, 33).Should().Be(99); // (300*33+50)/100 = 9950/100 = 99
    }

    [Fact]
    public void Percent_to_halalas_rounds_half_up()
    {
        // 33% of 300 = 99 exactly
        DiscountResolver.PercentToHalalas(300, 33).Should().Be(99);
        // 33% of 301 = 99.33 → rounds to 99
        DiscountResolver.PercentToHalalas(301, 33).Should().Be(99);
        // 33% of 600 = 198 exactly
        DiscountResolver.PercentToHalalas(600, 33).Should().Be(198);
    }

    [Fact]
    public void Zero_original_amount_with_positive_discount_is_clamped()
    {
        var result = DiscountResolver.Resolve(0, 100, null, null);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0); // Clamped to original (0)
    }

    [Fact]
    public void Both_limits_null_allows_full_discount()
    {
        var halalas = DiscountResolver.PercentToHalalas(1000, 100);
        halalas.Should().Be(1000);
        var result = DiscountResolver.Resolve(1000, halalas, null, null);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1000); // Full amount allowed
    }

    [Fact]
    public void Max_percent_only_allows_within_fixed()
    {
        // Only max % set to 50%, no max fixed
        var halalas = DiscountResolver.PercentToHalalas(1000, 40);
        halalas.Should().Be(400);
        var result = DiscountResolver.Resolve(1000, halalas, maxPercent: 50, maxFixedHalalas: null);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(400); // Within 50% limit
    }

    [Fact]
    public void Max_fixed_only_allows_within_percent()
    {
        // Only max fixed set to 300, no max %
        var result = DiscountResolver.Resolve(1000, 250, maxPercent: null, maxFixedHalalas: 300);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(250); // Within fixed limit
    }

    [Fact]
    public void Max_percent_only_rejects_over_limit()
    {
        var halalas = DiscountResolver.PercentToHalalas(1000, 60);
        halalas.Should().Be(600);
        var result = DiscountResolver.Resolve(1000, halalas, maxPercent: 50, maxFixedHalalas: null);
        result.IsSuccess.Should().BeTrue();
        // Clamped to 50% of 1000 = 500
        result.Value.Should().Be(500);
    }

    [Fact]
    public void Max_fixed_only_rejects_over_limit()
    {
        var result = DiscountResolver.Resolve(1000, 400, maxPercent: null, maxFixedHalalas: 300);
        result.IsSuccess.Should().BeTrue();
        // Clamped to 300
        result.Value.Should().Be(300);
    }

    [Fact]
    public void Both_limits_exceeded_returns_error()
    {
        var halalas = DiscountResolver.PercentToHalalas(1000, 60);
        halalas.Should().Be(600);
        var result = DiscountResolver.Resolve(1000, halalas, maxPercent: 50, maxFixedHalalas: 500);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Both_limits_within_returns_value()
    {
        var halalas = DiscountResolver.PercentToHalalas(1000, 40);
        halalas.Should().Be(400);
        var result = DiscountResolver.Resolve(1000, halalas, maxPercent: 50, maxFixedHalalas: 500);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(400); // Within both limits
    }

    [Fact]
    public void Percent_at_boundary_of_max_percent_is_valid()
    {
        var halalas = DiscountResolver.PercentToHalalas(1000, 50);
        halalas.Should().Be(500);
        var result = DiscountResolver.Resolve(1000, halalas, maxPercent: 50, maxFixedHalalas: null);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(500); // Exactly at limit is valid
    }

    [Fact]
    public void Fixed_at_boundary_of_max_fixed_is_valid()
    {
        var result = DiscountResolver.Resolve(1000, 300, maxPercent: null, maxFixedHalalas: 300);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(300); // Exactly at limit is valid
    }

    [Fact]
    public void One_over_max_percent_but_within_fixed_is_valid()
    {
        // 51% of 1000 = 510, exceeds max % (50% = 500) but within fixed (600)
        var halalas = DiscountResolver.PercentToHalalas(1000, 51);
        halalas.Should().Be(510);
        var result = DiscountResolver.Resolve(1000, halalas, maxPercent: 50, maxFixedHalalas: 600);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(510); // Valid because within fixed limit
    }

    [Fact]
    public void One_over_max_fixed_but_within_percent_is_valid()
    {
        // 301 exceeds max fixed (300) but within max % (40% = 400)
        var result = DiscountResolver.Resolve(1000, 301, maxPercent: 40, maxFixedHalalas: 300);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(301); // Valid because within percent limit
    }

    [Fact]
    public void Both_over_limits_returns_error()
    {
        var halalas = DiscountResolver.PercentToHalalas(1000, 51);
        halalas.Should().Be(510);
        var result = DiscountResolver.Resolve(1000, halalas, maxPercent: 50, maxFixedHalalas: 500);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Large_original_amount_with_percent()
    {
        var halalas = DiscountResolver.PercentToHalalas(100000, 15);
        halalas.Should().Be(15000); // (100000 * 15 + 50) / 100 = 15000
        var result = DiscountResolver.Resolve(100000, halalas, null, null);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(15000);
    }

    [Fact]
    public void Small_original_amount_with_percent()
    {
        var halalas = DiscountResolver.PercentToHalalas(3, 100);
        halalas.Should().Be(3); // (3 * 100 + 50) / 100 = 3
        var result = DiscountResolver.Resolve(3, halalas, null, null);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
    }

    [Fact]
    public void Percent_to_halalas_zero_percent()
    {
        // (1000 * 0 + 50) / 100 = 0
        DiscountResolver.PercentToHalalas(1000, 0).Should().Be(0);
    }

    [Fact]
    public void Percent_to_halalas_100_percent()
    {
        DiscountResolver.PercentToHalalas(1000, 100).Should().Be(1000);
    }
}
