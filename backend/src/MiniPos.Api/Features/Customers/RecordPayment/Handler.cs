using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Features.Common;
using MiniPos.Domain.Common;
using MiniPos.Infrastructure.Persistence;

namespace MiniPos.Api.Features.Customers.RecordPayment;

public sealed class RecordPaymentHandler(AppDbContext db)
    : IRequestHandler<RecordPaymentCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = await db.Customers.SingleOrDefaultAsync(
            c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
        {
            return new Error("customer.not_found", "Customer not found.", ErrorKind.NotFound);
        }

        if (request.AmountHalalas > customer.CreditBalanceHalalas)
        {
            return new Error("payment.overpayment",
                $"Payment amount {request.AmountHalalas} exceeds the current balance {customer.CreditBalanceHalalas}.",
                ErrorKind.Validation);
        }

        customer.CreditBalanceHalalas -= request.AmountHalalas;

        await db.SaveChangesAsync(cancellationToken);

        return new CustomerResponse(customer.Id, customer.Name, customer.Phone, customer.CreditBalanceHalalas);
    }
}
