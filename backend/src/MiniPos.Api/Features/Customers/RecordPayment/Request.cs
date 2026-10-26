using MediatR;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;

namespace MiniPos.Api.Features.Customers.RecordPayment;

public sealed record RecordPaymentCommand(Guid CustomerId, long AmountHalalas, string? Note)
    : IRequest<Result<CustomerResponse>>;
