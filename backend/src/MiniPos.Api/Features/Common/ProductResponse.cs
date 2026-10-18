namespace MiniPos.Api.Features.Common;

public sealed record ProductResponse(Guid Id, string NameEn, string NameAr, string Sku, long PriceHalalas, int Stock);