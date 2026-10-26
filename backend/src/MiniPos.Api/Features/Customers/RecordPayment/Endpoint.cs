using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Customers.RecordPayment;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/customers/{id:guid}/payments", async (
                Guid id,
                RecordPaymentCommand command,
                ISender sender,
                CancellationToken ct) =>
            (await sender.Send(command with { CustomerId = id }, ct)).ToHttp(r => Results.Ok(r)))
            .AddEndpointFilter<ValidationFilter<RecordPaymentCommand>>()
            .RequireAuthorization(Policies.CompanyAdmin)
            .WithName("RecordCustomerPayment");
}
