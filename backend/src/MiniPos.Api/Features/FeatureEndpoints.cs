using MiniPos.Api.Features.Catalog.LowStockProducts;
using MiniPos.Api.Features.Customers.CreateCustomer;
using MiniPos.Api.Features.Customers.ListCustomers;
using MiniPos.Api.Features.Customers.UpdateCustomer;
using MiniPos.Api.Features.Reports.DailySales;
using MiniPos.Api.Features.Sales.GetReceipt;
using MiniPos.Api.Features.Sales.VoidSale;
using MiniPos.Api.Features.Sales.ListSales;
using MiniPos.Api.Features.Shifts;

namespace MiniPos.Api.Features;

/// <summary>Registers every slice's endpoint. Add new slices here.</summary>
public static class FeatureEndpoints
{
    public static void MapFeatures(this IEndpointRouteBuilder app)
    {
        Catalog.CreateProduct.Endpoint.Map(app);
        Catalog.ListProducts.Endpoint.Map(app);
        Catalog.UpdateProduct.Endpoint.Map(app);
        Catalog.LowStockProducts.Endpoint.Map(app);
        Sales.CreateSale.Endpoint.Map(app);
        Sales.GetSale.Endpoint.Map(app);
        Sales.GetReceipt.Endpoint.Map(app);
        Sales.VoidSale.Endpoint.Map(app);
        Sales.ListSales.Endpoint.Map(app);
        Reports.DailySales.Endpoint.Map(app);
        Customers.CreateCustomer.Endpoint.Map(app);
        Customers.UpdateCustomer.Endpoint.Map(app);
        Customers.ListCustomers.Endpoint.Map(app);
        OpenShiftEndpoint.Map(app);
        CloseShiftEndpoint.Map(app);
        GetShiftEndpoint.Map(app);
        ListShiftsEndpoint.Map(app);
    }
}
