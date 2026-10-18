using FluentValidation;
using FluentValidation.Validators;

namespace MiniPos.Api.Validation;

/// <summary>
/// Shared rules for free text. Rejected, never stripped: stripping would silently change what the user typed.
/// Tests.Architecture/ValidationConventions checks that every *Name rule uses them.
/// </summary>
public static class TextRules
{
    /// <summary>Rejects C0/C1 control characters (e.g. NUL), which otherwise reach Postgres and fail as a 500.</summary>
    public static IRuleBuilderOptions<T, string?> NoControlCharacters<T>(this IRuleBuilder<T, string?> rule) =>
        rule.SetValidator(new NoControlCharactersValidator<T>());

    /// <summary>Rejects Unicode bidi control characters (U+202A-U+202E, U+2066-U+2069), which can be used to spoof names.</summary>
    public static IRuleBuilderOptions<T, string?> NoBidiControls<T>(this IRuleBuilder<T, string?> rule) =>
        rule.SetValidator(new NoBidiControlsValidator<T>());
}

public sealed class NoControlCharactersValidator<T> : PropertyValidator<T, string?>
{
    public override string Name => "NoControlCharacters";

    public override bool IsValid(ValidationContext<T> context, string? value) =>
        value is null || !value.Any(char.IsControl);

    protected override string GetDefaultMessageTemplate(string errorCode) =>
        "'{PropertyName}' must not contain control characters.";
}

public sealed class NoBidiControlsValidator<T> : PropertyValidator<T, string?>
{
    public override string Name => "NoBidiControls";

    public override bool IsValid(ValidationContext<T> context, string? value)
    {
        if (value is null)
            return true;

        // Bidi control characters to reject (U+202A-U+202E, U+2066-U+2069)
        // U+200E (LRM), U+200F (RLM), U+061C (ALM) are allowed as they are weak marks
        foreach (char c in value)
        {
            if ((c >= 0x202A && c <= 0x202E) || (c >= 0x2066 && c <= 0x2069))
            {
                // Check if it's one of the allowed weak marks
                if (c == 0x200E || c == 0x200F || c == 0x061C)
                    continue;
                
                return false;
            }
        }

        return true;
    }

    protected override string GetDefaultMessageTemplate(string errorCode) =>
        "'{PropertyName}' must not contain bidi control characters.";
}
