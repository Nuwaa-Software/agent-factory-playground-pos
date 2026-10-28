using FluentValidation;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Company.UpdateSettings;

public sealed class UpdateSettingsValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsValidator()
    {
        RuleFor(x => x.TaxRatePercent)
            .InclusiveBetween(0, 100)
            .When(x => x.TaxRatePercent.HasValue);

        RuleFor(x => x.LowStockThreshold)
            .InclusiveBetween(0, 10000)
            .When(x => x.LowStockThreshold.HasValue);

        RuleFor(x => x.ReceiptFooterText)
            .MaximumLength(500)
            .Must((cmd, text) => text is null || !text.Any(char.IsControl))
            .WithMessage("'ReceiptFooterText' must not contain control characters.")
            .Must((cmd, text) => text is null || !HasBidiControls(text))
            .WithMessage("'ReceiptFooterText' must not contain bidi control characters.")
            .When(x => x.ReceiptFooterText is not null);

        RuleFor(x => x.CompanyName)
            .MaximumLength(500)
            .NoControlCharacters()
            .NoBidiControls()
            .When(x => x.CompanyName is not null);

        RuleFor(x => x.VatRegistrationNumber)
            .MaximumLength(500)
            .NoControlCharacters()
            .NoBidiControls()
            .When(x => x.VatRegistrationNumber is not null);

        static bool HasBidiControls(string value)
        {
            foreach (char c in value)
            {
                if ((c >= 0x202A && c <= 0x202E) || (c >= 0x2066 && c <= 0x2069))
                {
                    if (c == 0x200E || c == 0x200F || c == 0x061C)
                        continue;
                    return true;
                }
            }
            return false;
        }
    }
}
