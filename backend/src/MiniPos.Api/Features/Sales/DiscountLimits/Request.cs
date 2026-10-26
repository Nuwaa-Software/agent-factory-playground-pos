using MediatR;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Sales.DiscountLimits;

public sealed record GetDiscountLimitsQuery : IRequest<Result<DiscountLimitsResponse>>;

public sealed record DiscountLimitsResponse(int? MaxPercent, long? MaxFixedHalalas);

public sealed record UpdateDiscountLimitsCommand(int? MaxPercent, long? MaxFixedHalalas)
    : IRequest<Result<DiscountLimitsResponse>>;
