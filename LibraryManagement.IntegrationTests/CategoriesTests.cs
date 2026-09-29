using LibraryManagement.Api.DTOs.Books;
using LibraryManagement.Api.DTOs.Categories;
using LibraryManagement.Api.Entities;
using LibraryManagement.IntegrationTests.Utils;
using System.Net;

namespace LibraryManagement.IntegrationTests;

[TestFixture]
internal class CategoriesTests : IntegrationTestsBase
{
    protected override string DatabaseName => nameof(CategoriesTests);
    protected override string BaseUrl => "/api/categories";
    private const string CategoryNameHistory = "History";
    private const string CategoryNameBiography = "Biography";
    private const int NonExistentCategoryId = 99;

    [Test]
    public async Task GetById_ShouldReturnCategory()
    {
        var createdCategory = await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });

        var getResponse = await GetByIdAsync(createdCategory.Id);

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var fetchedCategory = await getResponse.ReadPayloadAsync<ReadCategoryDto>();

        Assert.That(fetchedCategory, Is.Not.Null);
        Assert.That(fetchedCategory.Name, Is.EqualTo(createdCategory.Name));
        Assert.That(fetchedCategory.Id, Is.EqualTo(createdCategory.Id));
    }

    [Test]
    public async Task GetById_ShouldReturnNotFound_WhenTheCategoryDoesNotExist()
    {
        var getResponse = await GetByIdAsync(NonExistentCategoryId);

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await getResponse.AssertThatProblemDetailsContain(
            NonExistentCategoryId.ToString());
    }

    [Test]
    public async Task GetAll_ShouldReturnAllCreatedCategories()
    {
        await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });
        await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameBiography });

        var response = await GetAllAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var categories = await response.ReadPayloadAsync<List<ReadCategoryDto>>();

        Assert.That(categories, Is.Not.Null);
        Assert.That(
            categories.Select(category => category.Name),
            Is.EqualTo([CategoryNameBiography, CategoryNameHistory]));
    }

    [Test]
    public async Task GetAll_ShouldReturnEmptyCollection_WhenNoCategoriesExist()
    {
        var response = await GetAllAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var categories = await response.ReadPayloadAsync<List<ReadCategoryDto>>();

        Assert.That(categories, Is.Not.Null);
        Assert.That(categories, Is.Empty);
    }

    [Test]
    public async Task Create_ShouldReturnCreated_WithCorrectData()
    {
        var createDto = new CreateCategoryDto { Name = CategoryNameHistory };

        var createResponse = await PostAsync(createDto);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var createdCategory =
            await createResponse.ReadPayloadAsync<ReadCategoryDto>();

        Assert.That(createdCategory, Is.Not.Null);
        Assert.That(createdCategory.Name, Is.EqualTo(createDto.Name));
        Assert.That(createdCategory.Id, Is.GreaterThan(0));

        Assert.That(createResponse.GetLocation(),
            Is.EqualTo($"{BaseUrl}/{createdCategory.Id}"));

        await AssertCategoryExistsInTheDatabase(
            createdCategory.Id, createdCategory.Name);
    }

    [Test]
    public async Task Create_ShouldReturnConflict_WhenNameIsAlreadyTaken()
    {
        await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });
        var createDto = new CreateCategoryDto { Name = CategoryNameHistory };

        var createResponse = await PostAsync(createDto);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));

        await createResponse.AssertThatProblemDetailsContain(
            CategoryNameHistory);
    }

    [Test]
    public async Task Create_ShouldReturnBadRequest_WhenNameIsInvalid()
    {
        var createDto = new CreateCategoryDto { Name = string.Empty };

        var createResponse = await PostAsync(createDto);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await createResponse.AssertThatValidationErrorsContain("Name");
    }

    [Test]
    public async Task Update_ShouldReturnUpdatedCategory()
    {
        var createdCategory = await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });

        var updateDto = new UpdateCategoryDto { Name = CategoryNameBiography };
        var updateResponse = await PutAsync(updateDto, createdCategory.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedCategory = await updateResponse.ReadPayloadAsync<ReadCategoryDto>();

        Assert.That(updatedCategory, Is.Not.Null);
        Assert.That(updatedCategory.Name, Is.EqualTo(CategoryNameBiography));
        Assert.That(updatedCategory.Id, Is.EqualTo(createdCategory.Id));

        await AssertCategoryExistsInTheDatabase(
            createdCategory.Id, CategoryNameBiography);
    }

    [Test]
    public async Task Update_ShouldReturnUpdatedCategory_WhenNameRemainsTheSame()
    {
        var createdCategory = await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });

        var updateDto = new UpdateCategoryDto { Name = CategoryNameHistory };
        var updateResponse = await PutAsync(updateDto, createdCategory.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedCategory = await updateResponse.ReadPayloadAsync<ReadCategoryDto>();

        Assert.That(updatedCategory, Is.Not.Null);
        Assert.That(updatedCategory.Name, Is.EqualTo(CategoryNameHistory));
        Assert.That(updatedCategory.Id, Is.EqualTo(createdCategory.Id));

        await AssertCategoryExistsInTheDatabase(
            createdCategory.Id, createdCategory.Name);
    }

    [Test]
    public async Task Update_ShouldReturnNotFound_WhenNoCategoryWithTheGivenIdExists()
    {
        var updateDto = new UpdateCategoryDto { Name = CategoryNameBiography };
        var updateResponse = await PutAsync(updateDto, NonExistentCategoryId);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await updateResponse.AssertThatProblemDetailsContain(
            NonExistentCategoryId.ToString());
    }

    [Test]
    public async Task Update_ShouldReturnConflict_WhenNewNameIsTaken()
    {
        await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameBiography });

        var createdCategory = await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });

        var updateDto = new UpdateCategoryDto { Name = CategoryNameBiography };
        var updateResponse = await PutAsync(updateDto, createdCategory.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await updateResponse.AssertThatProblemDetailsContain(CategoryNameBiography);

        await AssertCategoryExistsInTheDatabase(
            createdCategory.Id, createdCategory.Name);
    }

    [Test]
    public async Task Update_ShouldReturnBadRequest_WhenNameIsInvalid()
    {
        var createdCategory = await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });

        var updateDto = new UpdateCategoryDto { Name = string.Empty };

        var updateResponse = await PutAsync(updateDto, createdCategory.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await updateResponse.AssertThatValidationErrorsContain("Name");
    }

    [Test]
    public async Task Delete_ShouldReturnNoContent_AndRemoveCategory()
    {
        var createdCategory = await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });

        var deleteResponse = await DeleteAsync(createdCategory.Id);
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResponse = await GetByIdAsync(createdCategory.Id);
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_ShouldReturnNotFound_WhenNoCategoryWithTheGivenIdExists()
    {
        var deleteResponse = await DeleteAsync(NonExistentCategoryId);

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await deleteResponse.AssertThatProblemDetailsContain(
            NonExistentCategoryId.ToString());
    }

    [Test]
    public async Task Delete_ShouldReturnConflict_WhenCategoryHasBooks()
    {
        var createdCategory = await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });

        await TestDataFactory.CreateBookAsync(createdCategory.Id);

        var deleteResponse = await DeleteAsync(createdCategory.Id);

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await deleteResponse.AssertThatProblemDetailsContain(
            createdCategory.Id.ToString());

        await AssertCategoryExistsInTheDatabase(
            createdCategory.Id, createdCategory.Name);
    }

    [Test]
    public async Task GetBooksByCategory_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        var getBooksByCategoryResponse = await GetBooksByCategoryAsync(NonExistentCategoryId);

        Assert.That(getBooksByCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await getBooksByCategoryResponse.AssertThatProblemDetailsContain(
            NonExistentCategoryId.ToString());
    }

    [Test]
    public async Task GetBooksByCategory_ShouldReturnBooks_ForThatCategoryOnly()
    {
        var category1 = await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });
        var category2 = await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameBiography });

        await TestDataFactory.CreateBookAsync(category1.Id, title: "Book1");
        await TestDataFactory.CreateBookAsync(category1.Id, title: "Book2");
        await TestDataFactory.CreateBookAsync(category2.Id, title: "Book3");
        await TestDataFactory.CreateBookAsync(category2.Id, title: "Book4");

        var getBooksByCategoryResponse = await GetBooksByCategoryAsync(category1.Id);
        Assert.That(getBooksByCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var books = await getBooksByCategoryResponse.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(
            books.Select(book => book.Title),
            Is.EqualTo(["Book1", "Book2"]));

        getBooksByCategoryResponse.AssertThatPaginationHeadersAre(
            totalCount: 2, totalPages: 1);
    }

    [Test]
    public async Task GetBooksByCategory_ShouldReturnEmptyCollection_WhenCategoryHasNoBooks()
    {
        var category = await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });

        var getBooksByCategoryResponse = await GetBooksByCategoryAsync(category.Id);
        Assert.That(getBooksByCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var books = await getBooksByCategoryResponse.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(books, Is.Empty);

        getBooksByCategoryResponse.AssertThatPaginationHeadersAre(
            totalCount: 0, totalPages: 0);
    }

    [Test]
    public async Task GetBooksByCategory_ShouldRespectPagination_AndSetResponseHeaders()
    {
        var category = await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });

        for (int i = 1; i <= 5; ++i)
        {
            await TestDataFactory.CreateBookAsync(category.Id, title: $"Book{i}");
        }

        var getBooksByCategoryResponse = await GetBooksByCategoryAsync(
            category.Id, 2, 3);
        Assert.That(getBooksByCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var books = await getBooksByCategoryResponse.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(
            books.Select(book => book.Title),
            Is.EqualTo(["Book4", "Book5"]));

        getBooksByCategoryResponse.AssertThatPaginationHeadersAre(
            totalCount: 5, totalPages: 2);
    }

    [Test]
    public async Task GetBooksByCategory_ShouldReturnEmptyCollection_WhenPageNumberExceedsTotalPages()
    {
        var category = await CreateAsync<CreateCategoryDto, ReadCategoryDto>(
            new CreateCategoryDto { Name = CategoryNameHistory });

        for (int i = 1; i <= 5; ++i)
        {
            await TestDataFactory.CreateBookAsync(category.Id, title: $"Book{i}");
        }

        var getBooksByCategoryResponse = await GetBooksByCategoryAsync(
            category.Id, 3, 3);
        Assert.That(getBooksByCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var books = await getBooksByCategoryResponse.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(books, Is.Empty);

        getBooksByCategoryResponse.AssertThatPaginationHeadersAre(
            totalCount: 5, totalPages: 2);
    }

    private async Task AssertCategoryExistsInTheDatabase(int id, string categoryName)
    {
        var getCategoryResponse = await GetByIdAsync(id);
        Assert.That(getCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var categoryFromGetResponse = await getCategoryResponse.ReadPayloadAsync<ReadCategoryDto>();
        Assert.That(categoryFromGetResponse, Is.Not.Null);
        Assert.That(categoryFromGetResponse.Name, Is.EqualTo(categoryName));
    }

    protected async Task<HttpResponseMessage> GetBooksByCategoryAsync(
        int categoryId,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["pageNumber"] = pageNumber?.ToString(),
            ["pageSize"] = pageSize?.ToString()
        };

        return await GetWithQuery(
            $"{BaseUrl}/{categoryId}/books", queryParameters);
    }
}
