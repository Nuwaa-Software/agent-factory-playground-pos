using FluentValidation;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Users.CreateUser;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200).NoControlCharacters().NoBidiControls();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200).NoControlCharacters().NoBidiControls();
        RuleFor(x => x.Role).IsInEnum();
    }
}
