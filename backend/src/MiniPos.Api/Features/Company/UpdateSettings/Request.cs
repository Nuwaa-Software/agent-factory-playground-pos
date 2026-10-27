using MediatR;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Company.UpdateSettings;

public sealed record UpdateSettingsCommand(decimal? TaxRatePercent = null, string? ReceiptFooterText = null, int? LowStockThreshold = null, string? CompanyName = null, string? VatRegistrationNumber = null) : IRequest<Result<MiniPos.Api.Features.Company.GetSettings.CompanySettingsResponse>>;
