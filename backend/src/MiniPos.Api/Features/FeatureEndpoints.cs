using MiniPos.Api.Features.Reports.DailySales;
using MiniPos.Api.Features.Sales.GetReceipt;

namespace MiniPos.Api.Features;

/// <summary>Registers every slice's endpoint. Add new slices here.</summary>
public static class FeatureEndpoints
{
    public static void MapFeatures(this IEndpointRouteBuilder app)
    {
        Catalog.CreateProduct.Endpoint.Map(app);
        Catalog.ListProducts.Endpoint.Map(app);
        Catalog.UpdateProduct.Endpoint.Map(app);
        Sales.CreateSale.Endpoint.Map(app);
        Sales.GetSale.Endpoint.Map(app);
        Sales.GetReceipt.Endpoint.Map(app);
        Reports.DailySales.Endpoint.Map(app);
    }
}
