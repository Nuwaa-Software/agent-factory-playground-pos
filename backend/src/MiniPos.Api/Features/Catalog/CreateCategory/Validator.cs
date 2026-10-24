using FluentValidation;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Catalog.CreateCategory;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.NameEn)
            .NotEmpty()
            .MaximumLength(200)
            .NoControlCharacters()
            .NoBidiControls();

        RuleFor(x => x.NameAr)
            .NotEmpty()
            .MaximumLength(200)
            .NoControlCharacters()
            .NoBidiControls();

        RuleFor(x => x.Description)
            .MaximumLength(1024)
            .When(x => x.Description != null);
    }
}