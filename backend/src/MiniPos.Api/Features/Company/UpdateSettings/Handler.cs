using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Company.GetSettings;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Api.Features.Company.UpdateSettings;

public sealed class UpdateSettingsHandler(AppDbContext db, ITenantContext tenant) : IRequestHandler<UpdateSettingsCommand, Result<CompanySettingsResponse>>
{
    public async Task<Result<CompanySettingsResponse>> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Upsert each provided setting
        if (request.TaxRatePercent.HasValue)
        {
            await UpsertSetting(db, MiniPos.Domain.Companies.CompanySettings.TaxRatePercentKey, request.TaxRatePercent.Value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture), cancellationToken);
        }

        if (request.ReceiptFooterText is not null)
        {
            await UpsertSetting(db, MiniPos.Domain.Companies.CompanySettings.ReceiptFooterTextKey, request.ReceiptFooterText, cancellationToken);
        }

        if (request.LowStockThreshold.HasValue)
        {
            await UpsertSetting(db, MiniPos.Domain.Companies.CompanySettings.LowStockThresholdKey, request.LowStockThreshold.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), cancellationToken);
        }

        // Persist changes before reading back
        await db.SaveChangesAsync(cancellationToken);

        // Read back the full settings
        var taxRate = await GetDecimalSetting(MiniPos.Domain.Companies.CompanySettings.TaxRatePercentKey, MiniPos.Domain.Companies.CompanySettings.TaxRatePercentDefault, cancellationToken);
        var footerText = await GetStringSetting(MiniPos.Domain.Companies.CompanySettings.ReceiptFooterTextKey, MiniPos.Domain.Companies.CompanySettings.ReceiptFooterTextDefault, cancellationToken);
        var lowStockThreshold = await GetIntSetting(MiniPos.Domain.Companies.CompanySettings.LowStockThresholdKey, MiniPos.Domain.Companies.CompanySettings.LowStockThresholdDefault, cancellationToken);

        return Result<CompanySettingsResponse>.Success(new CompanySettingsResponse(taxRate, footerText, lowStockThreshold));
    }

    private async Task UpsertSetting(AppDbContext db, string key, string value, CancellationToken ct)
    {
        var config = await db.CompanyConfigs.FirstOrDefaultAsync(c => c.Key == key && c.CompanyId == tenant.CompanyId, ct);
        if (config != null)
        {
            config.Value = value;
        }
        else
        {
            db.CompanyConfigs.Add(new MiniPos.Domain.Companies.CompanyConfig
            {
                CompanyId = tenant.CompanyId,
                Key = key,
                Value = value,
            });
        }
    }

    private async Task<decimal> GetDecimalSetting(string key, decimal defaultValue, CancellationToken ct)
    {
        var config = await db.CompanyConfigs.FirstOrDefaultAsync(c => c.Key == key && c.CompanyId == tenant.CompanyId, ct);
        if (config != null && decimal.TryParse(config.Value, out var value))
        {
            return value;
        }
        return defaultValue;
    }

    private async Task<string> GetStringSetting(string key, string defaultValue, CancellationToken ct)
    {
        var config = await db.CompanyConfigs.FirstOrDefaultAsync(c => c.Key == key && c.CompanyId == tenant.CompanyId, ct);
        return config?.Value ?? defaultValue;
    }

    private async Task<int> GetIntSetting(string key, int defaultValue, CancellationToken ct)
    {
        var config = await db.CompanyConfigs.FirstOrDefaultAsync(c => c.Key == key && c.CompanyId == tenant.CompanyId, ct);
        if (config != null && int.TryParse(config.Value, out var value))
        {
            return value;
        }
        return defaultValue;
    }
}
