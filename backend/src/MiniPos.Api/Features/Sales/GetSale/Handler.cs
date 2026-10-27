using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Common;
using MiniPos.Domain.Sales;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Sales.GetSale;

public sealed class GetSaleHandler(AppDbContext db) : IRequestHandler<GetSaleQuery, Result<SaleResponse>>
{
    public async Task<Result<SaleResponse>> Handle(GetSaleQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Lines are an owned collection; load the sale with them in one query.
        var sale = await db.Sales
            .Include(s => s.Lines)
            .SingleOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (sale is null)
        {
            return new Error("sale.not_found", "Sale not found.", ErrorKind.NotFound);
        }

        var company = await db.Companies.SingleAsync(c => c.Id == sale.CompanyId, cancellationToken);

        // Load refunds for this sale
        var refunds = await db.Refunds
            .Where(r => r.SaleId == sale.Id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return SaleResponse.From(sale, company, refunds);
    }
}
