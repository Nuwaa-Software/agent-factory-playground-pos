using FluentValidation;
using System.Globalization;

namespace MiniPos.Api.Features.Reports.SalesAnalytics;

public sealed class SalesAnalyticsValidator : AbstractValidator<SalesAnalyticsQuery>
{
    public SalesAnalyticsValidator()
    {
        RuleFor(x => x.startDate)
            .Matches(@"^\d{4}-\d{2}-\d{2}$")
            .When(x => x.startDate != null)
            .WithMessage("startDate must be in YYYY-MM-DD format.")
            .DependentRules(() =>
            {
                RuleFor(x => x.startDate)
                    .Custom((date, context) =>
                    {
                        if (date != null && !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        {
                            context.AddFailure("startDate must be a valid calendar date.");
                        }
                    });
            });

        RuleFor(x => x.endDate)
            .Matches(@"^\d{4}-\d{2}-\d{2}$")
            .When(x => x.endDate != null)
            .WithMessage("endDate must be in YYYY-MM-DD format.")
            .DependentRules(() =>
            {
                RuleFor(x => x.endDate)
                    .Custom((date, context) =>
                    {
                        if (date != null && !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        {
                            context.AddFailure("endDate must be a valid calendar date.");
                        }
                    });
            });

        RuleFor(x => x.topN)
            .GreaterThan(0)
            .When(x => x.topN.HasValue)
            .WithMessage("topN must be greater than 0.")
            .LessThanOrEqualTo(50)
            .When(x => x.topN.HasValue)
            .WithMessage("topN must not exceed 50.");
    }
}
