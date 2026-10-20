using MiniPos.Domain.Sales;

namespace MiniPos.Tests.Unit;

public class SaleTests
{
    [Fact]
    public void New_Sale_Has_Cash_PaymentType_By_Default()
    {
        // Arrange
        var sale = new Sale();
        
        // Act & Assert
        sale.PaymentType.Should().Be(PaymentType.Cash);
    }
}