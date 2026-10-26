using MediatR;
using MiniPos.Domain.Companies;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Company.GetSettings;

public sealed record GetSettingsQuery() : IRequest<Result<CompanySettingsResponse>>;

public sealed record CompanySettingsResponse(decimal TaxRatePercent, string ReceiptFooterText, int LowStockThreshold);
