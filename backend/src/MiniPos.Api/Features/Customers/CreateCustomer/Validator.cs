using FluentValidation;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Customers.CreateCustomer;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).NoControlCharacters().NoBidiControls();
        RuleFor(x => x.Phone).MaximumLength(32);
    }
}
