using MediatR;

namespace MiniPos.Api.Features.Sales.ListSales;

public sealed record ListSalesQuery : IRequest<ListSalesResponse>
{
    public int Page { get; init; } = 1;
    
    public int PageSize { get; init; } = 20;
    
    public DateTimeOffset? From { get; init; }
    
    public DateTimeOffset? To { get; init; }
}

public sealed record ListSalesResponse(
    IReadOnlyList<SaleSummary> Sales,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public sealed record SaleSummary(
    Guid Id,
    int Number,
    DateTimeOffset CreatedAt,
    long NetHalalas,
    long VatHalalas,
    long TotalHalalas
);