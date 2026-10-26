namespace MiniPos.Api.Features.Common;

public sealed record CustomerResponse(Guid Id, string Name, string? Phone, long CreditBalanceHalalas);
