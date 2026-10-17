using MiniPos.Api.Features.Catalog.CreateProduct;

namespace MiniPos.Tests.Unit;

public class CreateProductValidatorTests
{
    private static readonly CreateProductValidator Validator = new();

    private static CreateProductCommand Valid() => new("Shirt", "قميص", "SHIRT-01", 5000, 10);

    [Fact]
    public void Valid_command_passes() => Validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void Nul_in_name_is_rejected() =>
        Validator.Validate(Valid() with { NameEn = "Sh\0irt" }).IsValid.Should().BeFalse();

    [Fact]
    public void Lowercase_sku_is_rejected() =>
        Validator.Validate(Valid() with { Sku = "shirt-01" }).IsValid.Should().BeFalse();

    [Fact]
    public void Negative_price_is_rejected() =>
        Validator.Validate(Valid() with { PriceHalalas = -1 }).IsValid.Should().BeFalse();
}
