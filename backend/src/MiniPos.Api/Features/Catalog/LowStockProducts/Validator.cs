using FluentValidation;

namespace MiniPos.Api.Features.Catalog.LowStockProducts;

public sealed class LowStockProductsValidator : AbstractValidator<LowStockProductsQuery>
{
    public LowStockProductsValidator()
    {
        RuleFor(x => x.Threshold)
            .GreaterThan(0)
            .When(x => x.Threshold.HasValue)
            .WithMessage("Threshold must be greater than 0.");
    }
}