using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Sales.DiscountLimits;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Sales.DiscountLimits;

public sealed class UpdateDiscountLimitsHandler(AppDbContext db, ITenantContext tenant) : IRequestHandler<UpdateDiscountLimitsCommand, Result<DiscountLimitsResponse>>
{
    private const string KeyMaxPercent = "Pos:MaxDiscountPercent";
    private const string KeyMaxFixedHalalas = "Pos:MaxDiscountFixedHalalas";

    public async Task<Result<DiscountLimitsResponse>> Handle(UpdateDiscountLimitsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate max percent if provided
        if (request.MaxPercent.HasValue && (request.MaxPercent.Value < 0 || request.MaxPercent.Value > 100))
        {
            return new Error("discount_limits.invalid_percent", "Max discount percent must be between 0 and 100.", ErrorKind.Validation);
        }

        // Validate max fixed if provided
        if (request.MaxFixedHalalas.HasValue && request.MaxFixedHalalas.Value < 0)
        {
            return new Error("discount_limits.invalid_fixed", "Max discount fixed amount must be non-negative.", ErrorKind.Validation);
        }

        // Upsert max percent
        var maxPercentConfig = await db.CompanyConfigs.Where(c => c.Key == KeyMaxPercent).FirstOrDefaultAsync(cancellationToken);
        if (request.MaxPercent.HasValue)
        {
            if (maxPercentConfig != null)
            {
                maxPercentConfig.Value = request.MaxPercent.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                db.CompanyConfigs.Add(new MiniPos.Domain.Companies.CompanyConfig
                {
                    CompanyId = tenant.CompanyId,
                    Key = KeyMaxPercent,
                    Value = request.MaxPercent.Value.ToString(CultureInfo.InvariantCulture)
                });
            }
        }
        else if (maxPercentConfig != null)
        {
            // Clear the limit
            db.CompanyConfigs.Remove(maxPercentConfig);
        }

        // Upsert max fixed
        var maxFixedConfig = await db.CompanyConfigs.Where(c => c.Key == KeyMaxFixedHalalas).FirstOrDefaultAsync(cancellationToken);
        if (request.MaxFixedHalalas.HasValue)
        {
            if (maxFixedConfig != null)
            {
                maxFixedConfig.Value = request.MaxFixedHalalas.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                db.CompanyConfigs.Add(new MiniPos.Domain.Companies.CompanyConfig
                {
                    CompanyId = tenant.CompanyId,
                    Key = KeyMaxFixedHalalas,
                    Value = request.MaxFixedHalalas.Value.ToString(CultureInfo.InvariantCulture)
                });
            }
        }
        else if (maxFixedConfig != null)
        {
            // Clear the limit
            db.CompanyConfigs.Remove(maxFixedConfig);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new DiscountLimitsResponse(request.MaxPercent, request.MaxFixedHalalas);
    }
}
