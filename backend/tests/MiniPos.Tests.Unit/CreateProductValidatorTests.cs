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
    public void Bidi_control_characters_in_name_are_rejected()
    {
        // Test all 9 bidi control characters
        var bidiControls = new[] { '\u202A', '\u202B', '\u202C', '\u202D', '\u202E', '\u2066', '\u2067', '\u2068', '\u2069' };
        
        foreach (var bidiControl in bidiControls)
        {
            Validator.Validate(Valid() with { NameEn = $"Sh{bidiControl}irt" }).IsValid.Should().BeFalse();
            Validator.Validate(Valid() with { NameAr = $"قميص{bidiControl}" }).IsValid.Should().BeFalse();
        }
    }

    [Fact]
    public void Allowed_weak_marks_in_name_are_accepted()
    {
        // Test that LRM (U+200E), RLM (U+200F), ALM (U+061C) are allowed
        var allowedMarks = new[] { '\u200E', '\u200F', '\u061C' };
        
        foreach (var allowedMark in allowedMarks)
        {
            Validator.Validate(Valid() with { NameEn = $"Sh{allowedMark}irt" }).IsValid.Should().BeTrue();
            Validator.Validate(Valid() with { NameAr = $"قميص{allowedMark}" }).IsValid.Should().BeTrue();
        }
    }

    [Fact]
    public void Lowercase_sku_is_rejected() =>
        Validator.Validate(Valid() with { Sku = "shirt-01" }).IsValid.Should().BeFalse();

    [Fact]
    public void Negative_price_is_rejected() =>
        Validator.Validate(Valid() with { PriceHalalas = -1 }).IsValid.Should().BeFalse();
}
