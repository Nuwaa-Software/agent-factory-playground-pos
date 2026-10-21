using FluentValidation;

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
        });
        
        // Validate that if PaymentType is provided, it's a valid enum value
        // This will catch invalid string values during deserialization
        RuleFor(x => x.PaymentType).IsInEnum();
    }
}
