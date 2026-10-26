using MediatR;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Company.UpdateSettings;

public sealed record UpdateSettingsCommand(decimal? TaxRatePercent = null, string? ReceiptFooterText = null, int? LowStockThreshold = null) : IRequest<Result<MiniPos.Api.Features.Company.GetSettings.CompanySettingsResponse>>;
