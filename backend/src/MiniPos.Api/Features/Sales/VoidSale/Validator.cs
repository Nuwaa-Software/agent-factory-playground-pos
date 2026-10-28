using FluentValidation;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Sales.VoidSale;

public sealed class VoidSaleRequestValidator : AbstractValidator<VoidSaleRequest>
{
    public VoidSaleRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .NoControlCharacters()
            .NoBidiControls()
            .When(x => x.Reason is not null);
    }
}
