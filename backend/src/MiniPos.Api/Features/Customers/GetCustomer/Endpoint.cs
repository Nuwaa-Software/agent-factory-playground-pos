using MediatR;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;

namespace MiniPos.Api.Features.Customers.GetCustomer;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/customers/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetCustomerQuery(id), ct)).ToHttp(Results.Ok))
            .RequireAuthorization(Policies.CompanyAdmin)
            .WithName("GetCustomer");
}
