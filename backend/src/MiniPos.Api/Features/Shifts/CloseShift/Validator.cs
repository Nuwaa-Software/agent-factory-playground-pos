using FluentValidation;

namespace MiniPos.Api.Features.Shifts.CloseShift;

public class CloseShiftValidator : AbstractValidator<CloseShiftCommand>
{
    public CloseShiftValidator()
    {
        RuleFor(x => x.ActualCashHalalas)
            .GreaterThanOrEqualTo(0);
    }
}