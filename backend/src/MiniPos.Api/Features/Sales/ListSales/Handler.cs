using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Sales.ListSales;

public sealed class ListSalesHandler(AppDbContext db) : IRequestHandler<ListSalesQuery, ListSalesResponse>
{
    public async Task<ListSalesResponse> Handle(ListSalesQuery request, CancellationToken cancellationToken)
    {
        // Clamp page to be at least 1
        var page = Math.Max(1, request.Page);
        
        // Clamp page size to be between 1 and 100
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        
        // Get total count
        var totalCount = await db.Sales.CountAsync(cancellationToken);
        
        // Calculate total pages
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        // If there are no sales, return empty list
        if (totalCount == 0)
        {
            return new ListSalesResponse(
                Sales: [],
                Page: page,
                PageSize: pageSize,
                TotalCount: totalCount,
                TotalPages: totalPages
            );
        }
        
        // Get the paginated data
        var sales = await db.Sales
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SaleSummary(
                Id: s.Id,
                Number: s.Number,
                CreatedAt: s.CreatedAt,
                NetHalalas: s.NetHalalas,
                VatHalalas: s.VatHalalas,
                TotalHalalas: s.TotalHalalas
            ))
            .ToListAsync(cancellationToken);
        
        return new ListSalesResponse(
            Sales: sales,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            TotalPages: totalPages
        );
    }
}