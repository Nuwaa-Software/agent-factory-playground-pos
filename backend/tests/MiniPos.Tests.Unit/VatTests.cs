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
}
