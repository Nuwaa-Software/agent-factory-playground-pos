using System.Globalization;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using MiniPos.Api.Auth;
using MiniPos.Api.Common;
using MiniPos.Api.Validation;

namespace MiniPos.Api.Features.Sales.CreateSale;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/pos/sales", async (CreateSaleCommand command, HttpContext httpContext, ISender sender, CancellationToken ct) =>
        {
            // Admin or Cashier may apply discounts: if any discount is present and the user is neither, reject with 403
            var role = httpContext.User.FindFirstValue(ClaimTypes.Role);
            bool hasDiscount = command.Lines.Any(l => l.Discount is not null) || command.SaleDiscount is not null;
            if (hasDiscount && role is not (Roles.Admin or Roles.Cashier))
            {
                return Results.Forbid();
            }

            var result = await sender.Send(command, ct);
            return result.ToHttp(s => Results.Created($"/api/pos/sales/{s.Id}", s));
        })
        .AddEndpointFilter<ValidationFilter<CreateSaleCommand>>()
        .RequireAuthorization(Policies.PosCashier)
        .WithName("CreateSale");
}
