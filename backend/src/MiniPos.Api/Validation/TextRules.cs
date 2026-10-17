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
}

public sealed class NoControlCharactersValidator<T> : PropertyValidator<T, string?>
{
    public override string Name => "NoControlCharacters";

    public override bool IsValid(ValidationContext<T> context, string? value) =>
        value is null || !value.Any(char.IsControl);

    protected override string GetDefaultMessageTemplate(string errorCode) =>
        "'{PropertyName}' must not contain control characters.";
}
