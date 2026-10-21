using FluentValidation;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Shifts.OpenShift;

public class OpenShiftValidator : AbstractValidator<OpenShiftCommand>
{
    public OpenShiftValidator()
    {
        RuleFor(x => x.CashierName)
            .NotEmpty()
            .MaximumLength(100)
            .NoControlCharacters()
            .NoBidiControls();

        RuleFor(x => x.StartingCashHalalas)
            .GreaterThanOrEqualTo(0);
    }
}