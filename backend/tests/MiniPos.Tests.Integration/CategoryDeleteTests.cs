using System.Net;
using System.Net.Http.Json;
using MiniPos.Api.Features.Catalog.CreateCategory;
using MiniPos.Api.Features.Catalog.CreateProduct;
using MiniPos.Api.Features.Catalog.UpdateCategory;
using MiniPos.Api.Features.Common;

namespace MiniPos.Tests.Integration;

public class CategoryDeleteTests(PostgresFixture db) : ApiTestBase(db)
{
    #region Delete endpoint tests

    [Fact]
    public async Task Delete_empty_category_returns_204()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a category with no products and no children
        var createResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        category.Should().NotBeNull();

        // Delete it
        var deleteResponse = await client.DeleteAsync($"/api/inventory/categories/{category!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_category_with_products_returns_409()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a category and a product in it
        var createCategoryResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        category.Should().NotBeNull();

        await client.PostAsJsonAsync("/api/inventory/products",
            new CreateProductCommand("Smartphone", "هاتف ذكي", "PHONE-01", 50000, 10, category!.Id));

        // Delete the category — should fail with 409
        var deleteResponse = await client.DeleteAsync($"/api/inventory/categories/{category.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await deleteResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Cannot delete category");
        body.Should().Contain("products");
    }

    [Fact]
    public async Task Delete_category_with_children_returns_409()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a parent category
        var createParentResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var parent = await createParentResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        parent.Should().NotBeNull();

        // Create a child category under it
        var createChildResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Phones", "هواتف", null, ParentId: parent!.Id));
        createChildResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Delete the parent — should fail with 409
        var deleteResponse = await client.DeleteAsync($"/api/inventory/categories/{parent.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await deleteResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Cannot delete category");
        body.Should().Contain("child categories");
    }

    [Fact]
    public async Task Delete_unknown_category_returns_404()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var unknownId = Guid.NewGuid();
        var deleteResponse = await client.DeleteAsync($"/api/inventory/categories/{unknownId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Hierarchy tests

    [Fact]
    public async Task CreateCategory_with_parentId_persists_parent()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create parent
        var createParentResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var parent = await createParentResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        parent.Should().NotBeNull();

        // Create child with parentId
        var createChildResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Phones", "هواتف", null, ParentId: parent!.Id));
        createChildResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var child = await createChildResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        child.Should().NotBeNull();
        child!.ParentId.Should().Be(parent.Id);

        // Verify via GET
        var getResponse = await client.GetAsync("/api/inventory/categories");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await getResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        categories.Should().NotBeNull();
        var foundChild = categories!.First(c => c.Id == child.Id);
        foundChild.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task CreateCategory_with_invalid_parent_returns_404()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        var fakeParentId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Phones", "هواتف", null, ParentId: fakeParentId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_can_set_parent()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create two root categories
        var create1Response = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var cat1 = await create1Response.Content.ReadFromJsonAsync<CategoryResponse>();

        var create2Response = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Clothing", "ملابس", null));
        var cat2 = await create2Response.Content.ReadFromJsonAsync<CategoryResponse>();

        // Create a root category, then set its parent via PATCH
        var createChildResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Phones", "هواتف", null));
        var child = await createChildResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var updateResponse = await client.PatchAsJsonAsync($"/api/inventory/categories/{child!.Id}",
            new UpdateCategoryCommand(child.Id, null, null, null, ParentId: cat1!.Id));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        updated.Should().NotBeNull();
        updated!.ParentId.Should().Be(cat1.Id);
    }

    [Fact]
    public async Task UpdateCategory_can_change_parent()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create two root categories
        var create1Response = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var cat1 = await create1Response.Content.ReadFromJsonAsync<CategoryResponse>();

        var create2Response = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Clothing", "ملابس", null));
        var cat2 = await create2Response.Content.ReadFromJsonAsync<CategoryResponse>();

        // Create a child under cat1
        var createChildResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Phones", "هواتف", null, ParentId: cat1!.Id));
        var child = await createChildResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        // Change parent to cat2
        var updateResponse = await client.PatchAsJsonAsync($"/api/inventory/categories/{child!.Id}",
            new UpdateCategoryCommand(child.Id, null, null, null, ParentId: cat2!.Id));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        updated!.ParentId.Should().Be(cat2.Id);
    }

    [Fact]
    public async Task UpdateCategory_can_clear_parent()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create parent and child
        var createParentResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var parent = await createParentResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var createChildResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Phones", "هواتف", null, ParentId: parent!.Id));
        var child = await createChildResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        // Clear parent via ClearParentId flag
        var updateResponse = await client.PatchAsJsonAsync($"/api/inventory/categories/{child!.Id}",
            new UpdateCategoryCommand(child.Id, null, null, null, ClearParentId: true));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        updated!.ParentId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCategory_self_parent_returns_400()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a category
        var createResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var category = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        // Try to set itself as parent
        var updateResponse = await client.PatchAsJsonAsync($"/api/inventory/categories/{category!.Id}",
            new UpdateCategoryCommand(category.Id, null, null, null, ParentId: category.Id));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompanyA_cannot_delete_CompanyB_category()
    {
        var a = await Factory.CreateCompanyAsync("A", "أ");
        var b = await Factory.CreateCompanyAsync("B", "ب");

        // Create category in company B
        var clientB = Factory.ClientFor(b);
        var createResponse = await clientB.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var category = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        // Try to delete from company A
        var clientA = Factory.ClientFor(a);
        var deleteResponse = await clientA.DeleteAsync($"/api/inventory/categories/{category!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompanyA_cannot_set_CompanyB_category_as_parent()
    {
        var a = await Factory.CreateCompanyAsync("A", "أ");
        var b = await Factory.CreateCompanyAsync("B", "ب");

        // Create category in company B
        var clientB = Factory.ClientFor(b);
        var createResponse = await clientB.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));
        var parent = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        // Try to use it as parent from company A
        var clientA = Factory.ClientFor(a);
        var response = await clientA.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Phones", "هواتف", null, ParentId: parent!.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_categories_includes_parentId()
    {
        var company = await Factory.CreateCompanyAsync();
        var client = Factory.ClientFor(company);

        // Create a root category (parentId should be null)
        await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Electronics", "إلكترونيات", null));

        // Create a child category without parent first
        var createChildResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Phones", "هواتف", null));
        var child = await createChildResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        // Now create parent and update child to have parent
        var createParentResponse = await client.PostAsJsonAsync("/api/inventory/categories",
            new CreateCategoryCommand("Root", "الجذر", null));
        var parent = await createParentResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        // Update child to have parent
        await client.PatchAsJsonAsync($"/api/inventory/categories/{child!.Id}",
            new UpdateCategoryCommand(child.Id, null, null, null, ParentId: parent!.Id));

        var getResponse = await client.GetAsync("/api/inventory/categories");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var categories = await getResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        categories.Should().NotBeNull();

        var foundParent = categories!.First(c => c.Id == parent.Id);
        foundParent.ParentId.Should().BeNull();

        var foundChild = categories!.First(c => c.Id == child.Id);
        foundChild.ParentId.Should().Be(parent.Id);
    }

    #endregion
}
