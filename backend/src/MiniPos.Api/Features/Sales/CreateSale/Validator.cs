using FluentValidation;
using MiniPos.Domain.Sales;

namespace MiniPos.Api.Features.Sales.CreateSale;

public sealed class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.Lines).NotEmpty();
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("Each product may appear only once per sale.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, 999);
            line.RuleFor(l => l.Discount)
                .SetValidator((IValidator<LineDiscountRequest?>)new LineDiscountValidator())
                .When((l, d) => d is not null);
        });

        RuleFor(x => x.SaleDiscount)
            .SetValidator((IValidator<SaleDiscountRequest?>)new SaleDiscountValidator())
            .When((cmd, d) => d is not null);

        // Validate that if PaymentType is provided, it's a valid enum value
        RuleFor(x => x.PaymentType).IsInEnum();

        // If payment type is Mixed, breakdown is required
        RuleFor(x => x.PaymentBreakdown)
            .NotNull()
            .When(x => x.PaymentType == PaymentType.Mixed);

        // Validate each breakdown entry (FluentValidation handles null as empty)
        RuleForEach(x => x.PaymentBreakdown).ChildRules(bd =>
        {
            bd.RuleFor(x => x.PaymentType).IsInEnum();
            bd.RuleFor(x => x.AmountHalalas).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class LineDiscountValidator : AbstractValidator<LineDiscountRequest?>
{
    public LineDiscountValidator()
    {
        RuleFor(x => x!.Type).IsInEnum();
        RuleFor(x => x!.Value).GreaterThan(0);
        RuleFor(x => x!.Reason).NotEmpty().WithMessage("A discount reason is required.");
        RuleFor(x => x!.Value)
            .LessThanOrEqualTo(100)
            .When(x => x!.Type == DiscountType.Percent)
            .WithMessage("Percent discount must be between 0 and 100.");
    }
}

public sealed class SaleDiscountValidator : AbstractValidator<SaleDiscountRequest?>
{
    public SaleDiscountValidator()
    {
        RuleFor(x => x!.Type).IsInEnum();
        RuleFor(x => x!.Value).GreaterThan(0);
        RuleFor(x => x!.Reason).NotEmpty().WithMessage("A discount reason is required.");
        RuleFor(x => x!.Value)
            .LessThanOrEqualTo(100)
            .When(x => x!.Type == DiscountType.Percent)
            .WithMessage("Percent discount must be between 0 and 100.");
    }
}
