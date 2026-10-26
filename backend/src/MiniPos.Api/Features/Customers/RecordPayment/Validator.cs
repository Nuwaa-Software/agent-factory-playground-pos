using FluentValidation;

namespace MiniPos.Api.Features.Customers.RecordPayment;

public sealed class RecordPaymentValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentValidator()
    {
        RuleFor(x => x.AmountHalalas).GreaterThan(0);
    }
}
