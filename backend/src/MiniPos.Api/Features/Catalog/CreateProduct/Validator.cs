using FluentValidation;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Catalog.CreateProduct;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200).NoControlCharacters().NoBidiControls();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200).NoControlCharacters().NoBidiControls();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64).Matches("^[A-Z0-9-]+$");
        RuleFor(x => x.PriceHalalas).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
    }
}
