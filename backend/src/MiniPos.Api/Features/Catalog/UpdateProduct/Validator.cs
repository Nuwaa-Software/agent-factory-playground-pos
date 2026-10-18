using FluentValidation;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Catalog.UpdateProduct;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.PriceHalalas).GreaterThanOrEqualTo(0).When(x => x.PriceHalalas.HasValue);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).When(x => x.Stock.HasValue);
    }
}