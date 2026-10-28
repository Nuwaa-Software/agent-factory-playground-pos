using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Company.GetSettings;

public sealed class GetSettingsHandler(AppDbContext db) : IRequestHandler<GetSettingsQuery, Result<CompanySettingsResponse>>
{
    public async Task<Result<CompanySettingsResponse>> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var taxRate = await GetDecimalSetting(db, MiniPos.Domain.Companies.CompanySettings.TaxRatePercentKey, MiniPos.Domain.Companies.CompanySettings.TaxRatePercentDefault, cancellationToken);
        var footerText = await GetStringSetting(db, MiniPos.Domain.Companies.CompanySettings.ReceiptFooterTextKey, MiniPos.Domain.Companies.CompanySettings.ReceiptFooterTextDefault, cancellationToken);
        var lowStockThreshold = await GetIntSetting(db, MiniPos.Domain.Companies.CompanySettings.LowStockThresholdKey, MiniPos.Domain.Companies.CompanySettings.LowStockThresholdDefault, cancellationToken);
        var companyName = await GetStringSetting(db, MiniPos.Domain.Companies.CompanySettings.CompanyNameKey, MiniPos.Domain.Companies.CompanySettings.CompanyNameDefault, cancellationToken);
        var vatRegistrationNumber = await GetStringSetting(db, MiniPos.Domain.Companies.CompanySettings.VatRegistrationNumberKey, MiniPos.Domain.Companies.CompanySettings.VatRegistrationNumberDefault, cancellationToken);

        return Result<CompanySettingsResponse>.Success(new CompanySettingsResponse(taxRate, footerText, lowStockThreshold, companyName, vatRegistrationNumber));
    }

    private static async Task<decimal> GetDecimalSetting(AppDbContext db, string key, decimal defaultValue, CancellationToken ct)
    {
        var config = await db.CompanyConfigs.FirstOrDefaultAsync(c => c.Key == key, ct);
        if (config != null && decimal.TryParse(config.Value, out var value))
        {
            return value;
        }
        return defaultValue;
    }

    private static async Task<string> GetStringSetting(AppDbContext db, string key, string defaultValue, CancellationToken ct)
    {
        var config = await db.CompanyConfigs.FirstOrDefaultAsync(c => c.Key == key, ct);
        return config?.Value ?? defaultValue;
    }

    private static async Task<int> GetIntSetting(AppDbContext db, string key, int defaultValue, CancellationToken ct)
    {
        var config = await db.CompanyConfigs.FirstOrDefaultAsync(c => c.Key == key, ct);
        if (config != null && int.TryParse(config.Value, out var value))
        {
            return value;
        }
        return defaultValue;
    }
}
