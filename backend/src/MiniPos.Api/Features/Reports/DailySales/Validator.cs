using FluentValidation;
using System.Globalization;

namespace MiniPos.Api.Features.Reports.DailySales;

public sealed class DailySalesValidator : AbstractValidator<DailySalesQuery>
{
    public DailySalesValidator()
    {
        RuleFor(x => x.date)
            .Matches(@"^\d{4}-\d{2}-\d{2}$")
            .When(x => x.date != null)
            .WithMessage("Date must be in YYYY-MM-DD format.")
            .DependentRules(() =>
            {
                RuleFor(x => x.date)
                    .Custom((date, context) =>
                    {
                        if (date != null && !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        {
                            context.AddFailure("Date must be a valid calendar date.");
                        }
                    });
            });
    }
}