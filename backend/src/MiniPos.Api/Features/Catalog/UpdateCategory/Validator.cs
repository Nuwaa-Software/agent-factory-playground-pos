using FluentValidation;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Catalog.UpdateCategory;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.NameEn)
            .MaximumLength(200)
            .NoControlCharacters()
            .NoBidiControls()
            .When(x => x.NameEn != null);

        RuleFor(x => x.NameAr)
            .MaximumLength(200)
            .NoControlCharacters()
            .NoBidiControls()
            .When(x => x.NameAr != null);

        RuleFor(x => x.Description)
            .MaximumLength(1024)
            .When(x => x.Description != null);
    }
}