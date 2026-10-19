using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Common;
using MiniPos.Domain.Invoicing;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Sales.GetReceipt;

public sealed class GetReceiptHandler(AppDbContext db, ITenantContext tenant) : IRequestHandler<GetReceiptQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetReceiptQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sale = await db.Sales.IgnoreQueryFilters().SingleOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (sale is null)
        {
            return new Error("sale.not_found", "Sale not found.", ErrorKind.NotFound);
        }

        if (sale.CompanyId != tenant.CompanyId)
        {
            // Cross-company access attempt - return 404 to avoid revealing existence
            return new Error("sale.not_found", "Sale not found.", ErrorKind.NotFound);
        }

        var company = await db.Companies.SingleAsync(c => c.Id == sale.CompanyId, cancellationToken);

        var receiptLines = new List<string>
        {
            $"{company.NameEn} / {company.NameAr}",
            $"VAT No: {company.VatNumber}",
            $"Sale #{sale.Number}  {sale.CreatedAt.UtcDateTime:yyyy-MM-dd HH:mm} UTC"
        };

        foreach (var line in sale.Lines)
        {
            var lineNet = InvoiceQr.FormatSar(line.LineNetHalalas);
            receiptLines.Add($"{line.Quantity} x {line.ProductNameEn} / {line.ProductNameAr}  {lineNet}");
        }

        receiptLines.Add($"Net: {InvoiceQr.FormatSar(sale.NetHalalas)}");
        receiptLines.Add($"VAT 15%: {InvoiceQr.FormatSar(sale.VatHalalas)}");
        receiptLines.Add($"Total: {InvoiceQr.FormatSar(sale.TotalHalalas)}");

        return string.Join("\n", receiptLines);
    }
}