using MiniPos.Domain.Sales;

namespace MiniPos.Tests.Unit;

public class VatTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 15)]
    [InlineData(1000, 150)]
    [InlineData(2500, 375)]
    public void Vat_is_15_percent_of_net(long net, long vat) => Vat.Compute(net).Should().Be(vat);

    [Theory]
    [InlineData(3, 0)]
    [InlineData(7, 1)]
    [InlineData(10, 2)]
    [InlineData(30, 5)]
    [InlineData(999, 150)]
    public void Vat_rounds_half_up(long net, long vat) => Vat.Compute(net).Should().Be(vat);

    [Fact]
    public void Negative_net_is_rejected() =>
        FluentActions.Invoking(() => Vat.Compute(-1)).Should().Throw<ArgumentOutOfRangeException>();

    // Tests for the decimal-rate overload
    [Fact]
    public void Vat_compute_with_rate_10_percent() =>
        Vat.Compute(1000, 10m).Should().Be(100);

    [Fact]
    public void Vat_compute_with_rate_20_percent() =>
        Vat.Compute(1000, 20m).Should().Be(200);

    [Fact]
    public void Vat_compute_with_rate_15_percent() =>
        Vat.Compute(3333, 15m).Should().Be(500);

    [Fact]
    public void Vat_compute_with_rate_small_net() =>
        Vat.Compute(7, 10m).Should().Be(1);

    [Fact]
    public void Vat_compute_with_rate_rounds_half_up()
    {
        // 10% of 5 = 0.5 halalas -> rounds up to 1
        Vat.Compute(5, 10.0m).Should().Be(1);
        // 10% of 4 = 0.4 halalas -> rounds down to 0
        Vat.Compute(4, 10.0m).Should().Be(0);
    }

    [Fact]
    public void Vat_compute_with_rate_negative_net_is_rejected() =>
        FluentActions.Invoking(() => Vat.Compute(-1, 10.0m)).Should().Throw<ArgumentOutOfRangeException>();
}
