using FluentValidation;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Sales.RefundSale;

public sealed class RefundSaleValidator : AbstractValidator<RefundSaleCommand>
{
    public RefundSaleValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();

        When(x => x.LineItems is not null && x.LineItems.Count > 0, () =>
        {
            RuleFor(x => x.LineItems!)
                .Must(lines => lines.Any())
                .WithMessage("At least one line item is required when specifying line items.");

            RuleForEach(x => x.LineItems!)
                .ChildRules(lines =>
                {
                    lines.RuleFor(l => l.SaleLineId).NotEmpty();
                    lines.RuleFor(l => l.Quantity).InclusiveBetween(1, 999);
                });

            RuleFor(x => x.LineItems!)
                .Must(lines =>
                {
                    var ids = lines.Select(l => l.SaleLineId).ToList();
                    return ids.Distinct().Count() == ids.Count;
                })
                .WithMessage("Duplicate sale line IDs are not allowed.");
        });

        When(x => x.Reason is not null, () =>
        {
            RuleFor(x => x.Reason)
                .NotEmpty()
                .NoControlCharacters()
                .NoBidiControls();
        });
    }
}
