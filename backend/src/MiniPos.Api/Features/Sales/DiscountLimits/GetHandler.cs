using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Sales.DiscountLimits;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Sales.DiscountLimits;

public sealed class GetDiscountLimitsHandler(AppDbContext db) : IRequestHandler<GetDiscountLimitsQuery, Result<DiscountLimitsResponse>>
{
    private const string KeyMaxPercent = "Pos:MaxDiscountPercent";
    private const string KeyMaxFixedHalalas = "Pos:MaxDiscountFixedHalalas";

    public async Task<Result<DiscountLimitsResponse>> Handle(GetDiscountLimitsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var maxPercentConfig = await db.CompanyConfigs.Where(c => c.Key == KeyMaxPercent).FirstOrDefaultAsync(cancellationToken);
        var maxFixedConfig = await db.CompanyConfigs.Where(c => c.Key == KeyMaxFixedHalalas).FirstOrDefaultAsync(cancellationToken);

        int? maxPercent = null;
        if (maxPercentConfig != null && int.TryParse(maxPercentConfig.Value, out var mp))
            maxPercent = mp;

        long? maxFixed = null;
        if (maxFixedConfig != null && long.TryParse(maxFixedConfig.Value, out var mf))
            maxFixed = mf;

        return new DiscountLimitsResponse(maxPercent, maxFixed);
    }
}
