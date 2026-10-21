using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Common;
using MiniPos.Domain.Sales;
using MiniPos.Domain.Companies;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Sales.VoidSale;

public sealed class VoidSaleHandler(AppDbContext db, ITenantContext tenant, TimeProvider clock) : IRequestHandler<VoidSaleCommand, Result<MiniPos.Api.Features.Sales.GetSale.SaleResponse>>
{
    public async Task<Result<MiniPos.Api.Features.Sales.GetSale.SaleResponse>> Handle(VoidSaleCommand request, CancellationToken cancellationToken)
    {
        // Load the sale with IgnoreQueryFilters to bypass tenant filter and do explicit company check
        var sale = await db.Sales
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (sale is null)
        {
            return new Error("sale.not_found", "Sale not found.", ErrorKind.NotFound);
        }

        // Tenant isolation check - if the sale doesn't belong to this company, return 404
        if (sale.CompanyId != tenant.CompanyId)
        {
            return new Error("sale.not_found", "Sale not found.", ErrorKind.NotFound);
        }

        sale.Voided = true;
        sale.VoidedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(cancellationToken);

        // Load the company to create the response
        var company = await db.Companies.FirstAsync(c => c.Id == sale.CompanyId, cancellationToken);
        
        return MiniPos.Api.Features.Sales.GetSale.SaleResponse.From(sale, company);
    }
}