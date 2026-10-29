using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateCategory;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Catalog.UpdateCategory;
using MiniPos.Api.Features.Catalog.UpdateProduct;
using MiniPos.Api.Features.Common;
using ProductResponse = MiniPos.Api.Features.Common.ProductResponse;

namespace MiniPos.Tests.Integration;

public class CategoryTests(PostgresFixture db) : ApiTestBase(db)
{
    [Fact]
    public async Task Admin_creates_category_returns_201_with_id()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", "Electronic devices"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        category.Should().NotBeNull();
        category!.Id.Should().NotBe(Guid.Empty);
        category.NameEn.Should().Be("Electronics");
        category.NameAr.Should().Be("إلكترونيات");
        category.Description.Should().Be("Electronic devices");
    }

    [Fact]
    public async Task Admin_updates_category_returns_200()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // First create a category
        var createResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", "Electronic devices"));
        var createdCategory = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        
        // Then update it
        var updateResponse = await client.PatchAsJsonAsync($"/api/inventory/categories/{createdCategory!.Id}",
            new UpdateCategoryCommand(createdCategory.Id, "Electronics Updated", null, "Updated description"));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCategory = await updateResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        updatedCategory.Should().NotBeNull();
        updatedCategory!.NameEn.Should().Be("Electronics Updated");
        updatedCategory.NameAr.Should().Be("إلكترونيات");
        updatedCategory.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task Update_unknown_category_returns_404()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var response = await client.PatchAsJsonAsync("/api/inventory/categories/00000000-0000-0000-0000-000000000000",
            new UpdateCategoryCommand(Guid.Empty, "Electronics", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_lists_categories_returns_200()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create some categories
        await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Clothing", "ملابس", null));

        var response = await client.GetAsync("/api/inventory/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        categories.Should().NotBeNull();
        categories!.Count.Should().Be(2);
        categories.Select(c => c.NameEn).Should().Contain(new[] { "Clothing", "Electronics" });
    }

    [Fact]
    public async Task CatalogRead_role_can_list_categories()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company, "Viewer");

        var response = await client.GetAsync("/api/inventory/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Viewer_cannot_create_category_returns_403()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company, "Viewer");

        var response = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Viewer_cannot_update_category_returns_403()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company, "Viewer");

        var response = await client.PatchAsJsonAsync("/api/inventory/categories/00000000-0000-0000-0000-000000000000",
            new UpdateCategoryCommand(Guid.Empty, "Electronics", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_gets_401()
    {
        var response = await Factory.CreateClient().GetAsync(new Uri("/api/inventory/categories", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CompanyA_cannot_see_CompanyB_categories()
    {
        var a = await Factory.CreateCompanyAsync("A", "أ");
        var b = await Factory.CreateCompanyAsync("B", "ب");
        
        // Create category in company A
        var clientA = Factory.ClientFor(a);
        await clientA.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));

        // Try to access categories from company B
        var clientB = Factory.ClientFor(b);
        var response = await clientB.GetAsync("/api/inventory/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        categories.Should().NotBeNull();
        categories!.Count.Should().Be(0);
    }

    [Fact]
    public async Task CompanyA_cannot_update_CompanyB_category()
    {
        var a = await Factory.CreateCompanyAsync("A", "أ");
        var b = await Factory.CreateCompanyAsync("B", "ب");
        
        // Create category in company B
        var clientB = Factory.ClientFor(b);
        var createResponse = await clientB.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var category = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        
        // Try to update that category from company A
        var clientA = Factory.ClientFor(a);
        var response = await clientA.PatchAsJsonAsync($"/api/inventory/categories/{category!.Id}",
            new UpdateCategoryCommand(category.Id, "Updated Name", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_with_category_id_persists_it()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // First create a category
        var createCategoryResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        
        // Then create a product with that category
        var createProductResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Smartphone", "هاتف ذكي", "PHONE-01", 50000, 10, category!.Id));
        
        createProductResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductResponse>();
        product.Should().NotBeNull();
        product!.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task UpdateProduct_can_change_category_id()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // First create two categories
        var createCategory1Response = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var category1 = await createCategory1Response.Content.ReadFromJsonAsync<CategoryResponse>();
        
        var createCategory2Response = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Clothing", "ملابس", null));
        var category2 = await createCategory2Response.Content.ReadFromJsonAsync<CategoryResponse>();
        
        // Then create a product without category
        var createProductResponse = await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Smartphone", "هاتف ذكي", "PHONE-01", 50000, 10));
        
        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductResponse>();
        
        // Update the product to assign it to a category
        var updateProductResponse = await client.PatchAsJsonAsync($"/api/inventory/products/{product!.Id}",
            new UpdateProductCommand(product.Id, null, null, category1!.Id, "Admin"));
            
        updateProductResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProduct = await updateProductResponse.Content.ReadFromJsonAsync<ProductResponse>();
        updatedProduct.Should().NotBeNull();
        updatedProduct!.CategoryId.Should().Be(category1.Id);
        
        // Update again to change to another category
        var updateProductResponse2 = await client.PatchAsJsonAsync($"/api/inventory/products/{product.Id}",
            new UpdateProductCommand(product.Id, null, null, category2!.Id, "Admin"));
            
        updateProductResponse2.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProduct2 = await updateProductResponse2.Content.ReadFromJsonAsync<ProductResponse>();
        updatedProduct2.Should().NotBeNull();
        updatedProduct2!.CategoryId.Should().Be(category2.Id);
    }

    [Fact]
    public async Task ListProducts_includes_category_id()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // First create a category
        var createCategoryResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        
        // Then create a product with that category
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Smartphone", "هاتف ذكي", "PHONE-01", 50000, 10, category!.Id));
            
        // Create a product without category
        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Laptop", "لابتوب", "LAPTOP-01", 80000, 5));
        
        var response = await client.GetAsync("/api/inventory/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        products.Should().NotBeNull();
        products!.Count.Should().Be(2);
        
        // One product should have a category ID, one should be null
        var productWithCategory = products.FirstOrDefault(p => p.CategoryId.HasValue);
        var productWithoutCategory = products.FirstOrDefault(p => !p.CategoryId.HasValue);
        
        productWithCategory.Should().NotBeNull();
        productWithCategory!.CategoryId.Should().Be(category.Id);
        
        productWithoutCategory.Should().NotBeNull();
        productWithoutCategory!.CategoryId.Should().BeNull();
    }
}