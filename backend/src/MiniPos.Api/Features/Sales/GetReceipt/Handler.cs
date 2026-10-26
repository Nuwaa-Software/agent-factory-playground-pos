using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Common;
using MiniPos.Domain.Companies;
using MiniPos.Domain.Invoicing;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Sales.GetReceipt;

public sealed class GetReceiptHandler(AppDbContext db, ITenantContext tenant) : IRequestHandler<GetReceiptQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetReceiptQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sale = await db.Sales.SingleOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
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

        // Compute the VAT rate used for this sale (for display)
        var taxRate = await GetTaxRate(sale.CompanyId, cancellationToken);
        var footerText = await GetFooterText(sale.CompanyId, cancellationToken);

        var receiptLines = new List<string>
        {
            $"{company.NameEn} / {company.NameAr}",
            $"VAT No: {company.VatNumber}",
            $"Sale #{sale.Number}  {sale.CreatedAt.UtcDateTime:yyyy-MM-dd HH:mm} UTC"
        };

        foreach (var line in sale.Lines)
        {
            var original = InvoiceQr.FormatSar(line.UnitPriceHalalas * line.Quantity);
            if (line.DiscountHalalas > 0)
            {
                receiptLines.Add($"{line.Quantity} x {line.ProductNameEn} / {line.ProductNameAr}  {original}");
                receiptLines.Add($"  Discount ({line.DiscountReason ?? ""}): -{InvoiceQr.FormatSar(line.DiscountHalalas)}");
                receiptLines.Add($"  Net: {InvoiceQr.FormatSar(line.LineNetHalalas)}");
            }
            else
            {
                var lineNet = InvoiceQr.FormatSar(line.LineNetHalalas);
                receiptLines.Add($"{line.Quantity} x {line.ProductNameEn} / {line.ProductNameAr}  {lineNet}");
            }
        }

        // Sale-level discount
        if (sale.SaleDiscountHalalas > 0)
        {
            var subtotal = InvoiceQr.FormatSar(sale.Lines.Sum(l => l.LineNetHalalas));
            receiptLines.Add($"Subtotal: {subtotal}");
            receiptLines.Add($"Sale Discount ({sale.SaleDiscountReason ?? ""}): -{InvoiceQr.FormatSar(sale.SaleDiscountHalalas)}");
            receiptLines.Add($"Net: {InvoiceQr.FormatSar(sale.NetHalalas)}");
        }
        else
        {
            receiptLines.Add($"Net: {InvoiceQr.FormatSar(sale.NetHalalas)}");
        }

        var taxRateStr = taxRate == Math.Floor(taxRate) ? ((int)taxRate).ToString(System.Globalization.CultureInfo.InvariantCulture) : taxRate.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        receiptLines.Add($"VAT {taxRateStr}%: {InvoiceQr.FormatSar(sale.VatHalalas)}");
        receiptLines.Add($"Total: {InvoiceQr.FormatSar(sale.TotalHalalas)}");

        if (!string.IsNullOrEmpty(footerText))
        {
            receiptLines.Add(footerText);
        }

        return string.Join("\n", receiptLines);
    }

    private async Task<decimal> GetTaxRate(Guid companyId, CancellationToken ct)
    {
        var config = await db.CompanyConfigs.FirstOrDefaultAsync(c => c.Key == CompanySettings.TaxRatePercentKey && c.CompanyId == companyId, ct);
        if (config != null && decimal.TryParse(config.Value, out var rate))
        {
            return rate;
        }
        return CompanySettings.TaxRatePercentDefault;
    }

    private async Task<string> GetFooterText(Guid companyId, CancellationToken ct)
    {
        var config = await db.CompanyConfigs.FirstOrDefaultAsync(c => c.Key == CompanySettings.ReceiptFooterTextKey && c.CompanyId == companyId, ct);
        return config?.Value ?? CompanySettings.ReceiptFooterTextDefault;
    }
}
