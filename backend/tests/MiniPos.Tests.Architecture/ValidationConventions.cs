using FluentValidation;
using MiniPos.Api.Validation;

namespace MiniPos.Tests.Architecture;

/// <summary>Every free-text *Name rule must reject control characters (nuwaa-pos #1057: a NUL in a name was a 500).</summary>
public class ValidationConventions
{
    public static TheoryData<Type> Validators()
    {
        var data = new TheoryData<Type>();
        foreach (var type in typeof(Program).Assembly.GetTypes()
                     .Where(t => !t.IsAbstract && t.BaseType is { IsGenericType: true } b &&
                                 b.GetGenericTypeDefinition() == typeof(AbstractValidator<>)))
        {
            data.Add(type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Validators))]
    public void Name_rules_reject_control_characters(Type validatorType)
    {
        var validator = (IValidator)Activator.CreateInstance(validatorType)!;
        var descriptor = validator.CreateDescriptor();
        foreach (var member in descriptor.GetMembersWithValidators().Select(g => g.Key)
                     .Where(m => m.EndsWith("Name", StringComparison.Ordinal) ||
                                 m.EndsWith("NameEn", StringComparison.Ordinal) ||
                                 m.EndsWith("NameAr", StringComparison.Ordinal)))
        {
            var rules = descriptor.GetValidatorsForMember(member).Select(v => v.Validator.GetType().Name);
            rules.Should().Contain(n => n.StartsWith("NoControlCharactersValidator", StringComparison.Ordinal),
                $"{validatorType.Name}.{member} must use TextRules.NoControlCharacters()");
        }
    }

    [Fact]
    public void There_are_validators_to_check() => Validators().Count.Should().BePositive();
}
