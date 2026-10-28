using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Sales.GetSale;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Sales.ListSales;

public sealed class ListSalesHandler(AppDbContext db, TimeProvider clock) : IRequestHandler<ListSalesQuery, ListSalesResponse>
{
    public async Task<ListSalesResponse> Handle(ListSalesQuery request, CancellationToken cancellationToken)
    {
        // Clamp page to be at least 1
        var page = Math.Max(1, request.Page);
        
        // Clamp page size to be between 1 and 100
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        
        // Apply date filtering if provided
        var query = db.Sales.AsQueryable();
        
        // Filter by customer if provided
        if (request.CustomerId.HasValue)
        {
            query = query.Where(s => s.CustomerId == request.CustomerId);
        }
        
        // Set default date range if not provided
        var from = request.From ?? clock.GetUtcNow().AddDays(-30);
        var to = request.To ?? clock.GetUtcNow();
        
        // Ensure from is not after to
        if (from > to)
        {
            // This should be handled by validation, but let's make sure
            throw new ArgumentException("From date cannot be after To date");
        }
        
        query = query.Where(s => s.CreatedAt >= from && s.CreatedAt <= to);
        
        // Apply status filter if provided
        if (request.Status != null)
        {
            var status = request.Status.ToLowerInvariant();
            if (status == "active")
            {
                query = query.Where(s => !s.Voided);
            }
            else if (status == "voided")
            {
                query = query.Where(s => s.Voided);
            }
            // For null, "all", or any other value: no additional filter
        }
        
        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);
        
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
        var sales = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.PaymentBreakdown)
            .ToListAsync(cancellationToken);

        return new ListSalesResponse(
            Sales: sales.Select(s => new SaleSummary(
                Id: s.Id,
                Number: s.Number,
                CreatedAt: s.CreatedAt,
                NetHalalas: s.NetHalalas,
                VatHalalas: s.VatHalalas,
                TotalHalalas: s.TotalHalalas,
                CustomerId: s.CustomerId,
                Voided: s.Voided,
                VoidedAt: s.VoidedAt,
                PaymentType: s.PaymentType,
                PaymentBreakdown: s.PaymentBreakdown.Select(e => new PaymentBreakdownResponse(
                    e.PaymentType, e.AmountHalalas)).ToList()
            )).ToList(),
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            TotalPages: totalPages
        );
    }
}
