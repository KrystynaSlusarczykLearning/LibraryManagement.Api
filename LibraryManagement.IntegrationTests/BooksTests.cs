using LibraryManagement.Api.DTOs.Books;
using LibraryManagement.Api.DTOs.Categories;
using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.IntegrationTests.Utils;
using System.Net;

namespace LibraryManagement.IntegrationTests;

[TestFixture]
internal class BooksTests : IntegrationTestsBase
{
    protected override string DatabaseName => nameof(BooksTests);
    protected override string BaseUrl => "/api/books";
    private const string TitleDune = "Dune";
    private const string TitleEmma = "Emma";
    private const string TitleHamlet = "Hamlet";
    private const string SampleEmail = "email1@gmail.com";
    private const int NonExistentId = 99;

    private ReadCategoryDto _category = null!;
    private ReadMemberDto _member = null!;

    [SetUp]
    public async Task CreateDefaultResources()
    {
        _category = await TestDataFactory.CreateCategoryAsync();
        _member = await TestDataFactory.CreateMemberAsync(email: SampleEmail);
    }

    [Test]
    public async Task GetById_ShouldReturnBook_WithCategoryAndAuthors()
    {
        var author = await TestDataFactory.CreateAuthorAsync("Frank", "Herbert");
        var createdBook = await TestDataFactory.CreateBookAsync(
            [author.Id], _category.Id, TitleDune, totalCopies: 4);

        var getResponse = await GetByIdAsync(createdBook.Id);

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var fetchedBook = await getResponse.ReadPayloadAsync<ReadBookDto>();

        Assert.That(fetchedBook, Is.Not.Null);
        Assert.That(fetchedBook.Id, Is.EqualTo(createdBook.Id));
        Assert.That(fetchedBook.Title, Is.EqualTo(TitleDune));
        Assert.That(fetchedBook.TotalCopies, Is.EqualTo(4));
        Assert.That(fetchedBook.AvailableCopies, Is.EqualTo(4));
        Assert.That(fetchedBook.CategoryId, Is.EqualTo(_category.Id));
        Assert.That(fetchedBook.CategoryName, Is.EqualTo(_category.Name));
        Assert.That(
            fetchedBook.Authors.Select(author => (author.Id, author.FirstName, author.LastName)),
            Is.EqualTo([(author.Id, "Frank", "Herbert")]));
    }

    [Test]
    public async Task GetById_ShouldReturnNotFound_WhenTheBookDoesNotExist()
    {
        var getResponse = await GetByIdAsync(NonExistentId);

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await getResponse.AssertThatProblemDetailsContain(NonExistentId.ToString());
    }

    [Test]
    public async Task GetAll_ShouldReturnAllBooks_OrderedById_ByDefault()
    {
        await TestDataFactory.CreateBookAsync(_category.Id, TitleHamlet);
        await TestDataFactory.CreateBookAsync(_category.Id, TitleDune);
        await TestDataFactory.CreateBookAsync(_category.Id, TitleEmma);

        var response = await GetAllAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var books = await response.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(
            books.Select(book => book.Title),
            Is.EqualTo([TitleHamlet, TitleDune, TitleEmma]));

        response.AssertThatPaginationHeadersAre(totalCount: 3, totalPages: 1);
    }

    [Test]
    public async Task GetAll_ShouldReturnEmptyCollection_WhenNoBooksExist()
    {
        var response = await GetAllAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var books = await response.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(books, Is.Empty);

        response.AssertThatPaginationHeadersAre(totalCount: 0, totalPages: 0);
    }

    [Test]
    public async Task GetAll_ShouldRespectPagination_AndSetResponseHeaders()
    {
        for (int i = 1; i <= 5; ++i)
        {
            await TestDataFactory.CreateBookAsync(_category.Id, $"Book{i}");
        }

        var response = await GetWithQuery(BaseUrl, new()
        {
            ["pageNumber"] = "2",
            ["pageSize"] = "3"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var books = await response.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(
            books.Select(book => book.Title),
            Is.EqualTo(["Book4", "Book5"]));

        response.AssertThatPaginationHeadersAre(totalCount: 5, totalPages: 2);
    }

    [Test]
    public async Task GetAll_ShouldFilterByTitle_UsingContains()
    {
        await TestDataFactory.CreateBookAsync(_category.Id, "The Hobbit");
        await TestDataFactory.CreateBookAsync(_category.Id, TitleDune);
        await TestDataFactory.CreateBookAsync(_category.Id, "Hobbit Stories");

        var response = await GetWithQuery(BaseUrl, new() { ["title"] = "Hobbit" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var books = await response.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(
            books.Select(book => book.Title),
            Is.EqualTo(["The Hobbit", "Hobbit Stories"]));

        response.AssertThatPaginationHeadersAre(totalCount: 2, totalPages: 1);
    }

    [Test]
    public async Task GetAll_ShouldFilterByCategoryId()
    {
        var otherCategory = await TestDataFactory.CreateCategoryAsync("History");
        await TestDataFactory.CreateBookAsync(_category.Id, TitleDune);
        await TestDataFactory.CreateBookAsync(otherCategory.Id, TitleHamlet);

        var response = await GetWithQuery(BaseUrl,
            new() { ["categoryId"] = otherCategory.Id.ToString() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var books = await response.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(books.Select(book => book.Title), Is.EqualTo([TitleHamlet]));
    }

    [Test]
    public async Task GetAll_ShouldFilterByAuthorId_IncludingBooksWithMultipleAuthors()
    {
        var author1 = await TestDataFactory.CreateAuthorAsync("Terry", "Pratchett");
        var author2 = await TestDataFactory.CreateAuthorAsync("Neil", "Gaiman");
        await TestDataFactory.CreateBookAsync([author1.Id], _category.Id, "Mort");
        await TestDataFactory.CreateBookAsync([author1.Id, author2.Id], _category.Id, "Good Omens");
        await TestDataFactory.CreateBookAsync([author2.Id], _category.Id, "Coraline");

        var response = await GetWithQuery(BaseUrl,
            new() { ["authorId"] = author2.Id.ToString() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var books = await response.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(
            books.Select(book => book.Title),
            Is.EqualTo(["Good Omens", "Coraline"]));
    }

    [Test]
    public async Task GetAll_ShouldCombineFilters()
    {
        var otherCategory = await TestDataFactory.CreateCategoryAsync("History");
        await TestDataFactory.CreateBookAsync(_category.Id, "Rome Novel");
        await TestDataFactory.CreateBookAsync(otherCategory.Id, "Rome Chronicle");
        await TestDataFactory.CreateBookAsync(otherCategory.Id, "Greece Chronicle");

        var response = await GetWithQuery(BaseUrl, new()
        {
            ["title"] = "Rome",
            ["categoryId"] = otherCategory.Id.ToString()
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var books = await response.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(books.Select(book => book.Title), Is.EqualTo(["Rome Chronicle"]));
    }

    [TestCase("Asc", new[] { TitleDune, TitleEmma, TitleHamlet })]
    [TestCase("Desc", new[] { TitleHamlet, TitleEmma, TitleDune })]
    public async Task GetAll_ShouldSortByTitle(string sortDirection, string[] expectedTitles)
    {
        await TestDataFactory.CreateBookAsync(_category.Id, TitleEmma);
        await TestDataFactory.CreateBookAsync(_category.Id, TitleHamlet);
        await TestDataFactory.CreateBookAsync(_category.Id, TitleDune);

        var response = await GetWithQuery(BaseUrl, new()
        {
            ["sortBy"] = "Title",
            ["sortDirection"] = sortDirection
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var books = await response.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(books.Select(book => book.Title), Is.EqualTo(expectedTitles));
    }

    [TestCase("Asc", new[] { 1, 2, 5 })]
    [TestCase("Desc", new[] { 5, 2, 1 })]
    public async Task GetAll_ShouldSortByAvailableCopies(
        string sortDirection, int[] expectedAvailableCopies)
    {
        await TestDataFactory.CreateBookAsync(_category.Id, TitleEmma, totalCopies: 2);
        await TestDataFactory.CreateBookAsync(_category.Id, TitleHamlet, totalCopies: 5);
        await TestDataFactory.CreateBookAsync(_category.Id, TitleDune, totalCopies: 1);

        var response = await GetWithQuery(BaseUrl, new()
        {
            ["sortBy"] = "AvailableCopies",
            ["sortDirection"] = sortDirection
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var books = await response.ReadPayloadAsync<List<ReadBookDto>>();

        Assert.That(books, Is.Not.Null);
        Assert.That(
            books.Select(book => book.AvailableCopies),
            Is.EqualTo(expectedAvailableCopies));
    }

    [Test]
    public async Task Create_ShouldReturnCreated_WithAvailableCopiesEqualToTotalCopies()
    {
        var author = await TestDataFactory.CreateAuthorAsync();
        var createDto = new CreateBookDto
        {
            Title = TitleDune,
            TotalCopies = 4,
            CategoryId = _category.Id,
            AuthorIds = [author.Id]
        };

        var createResponse = await PostAsync(createDto);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var createdBook = await createResponse.ReadPayloadAsync<ReadBookDto>();

        Assert.That(createdBook, Is.Not.Null);
        Assert.That(createdBook.Id, Is.GreaterThan(0));
        Assert.That(createdBook.Title, Is.EqualTo(TitleDune));
        Assert.That(createdBook.TotalCopies, Is.EqualTo(4));
        Assert.That(createdBook.AvailableCopies, Is.EqualTo(4));
        Assert.That(createdBook.CategoryName, Is.EqualTo(_category.Name));
        Assert.That(createdBook.Authors.Select(author => author.Id), Is.EqualTo([author.Id]));

        Assert.That(createResponse.GetLocation(),
            Is.EqualTo($"{BaseUrl}/{createdBook.Id}"));

        var getResponse = await GetByIdAsync(createdBook.Id);
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Create_ShouldIgnoreDuplicatedAuthorIds()
    {
        var author = await TestDataFactory.CreateAuthorAsync();

        var createResponse = await PostAsync(new CreateBookDto
        {
            Title = TitleDune,
            TotalCopies = 1,
            CategoryId = _category.Id,
            AuthorIds = [author.Id, author.Id]
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var createdBook = await createResponse.ReadPayloadAsync<ReadBookDto>();

        Assert.That(createdBook, Is.Not.Null);
        Assert.That(createdBook.Authors.Select(author => author.Id), Is.EqualTo([author.Id]));
    }

    [Test]
    public async Task Create_ShouldReturnBadRequest_WhenCategoryDoesNotExist()
    {
        var author = await TestDataFactory.CreateAuthorAsync();

        var createResponse = await PostAsync(new CreateBookDto
        {
            Title = TitleDune,
            TotalCopies = 1,
            CategoryId = NonExistentId,
            AuthorIds = [author.Id]
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await createResponse.AssertThatProblemDetailsContain(NonExistentId.ToString());
    }

    [Test]
    public async Task Create_ShouldReturnBadRequest_WhenAnyAuthorDoesNotExist()
    {
        var author = await TestDataFactory.CreateAuthorAsync();

        var createResponse = await PostAsync(new CreateBookDto
        {
            Title = TitleDune,
            TotalCopies = 1,
            CategoryId = _category.Id,
            AuthorIds = [author.Id, NonExistentId]
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await createResponse.AssertThatProblemDetailsContain("author");
    }

    [Test]
    public async Task Create_ShouldReturnBadRequest_WhenDtoIsInvalid()
    {
        var author = await TestDataFactory.CreateAuthorAsync();

        var createResponse = await PostAsync(new CreateBookDto
        {
            Title = string.Empty,
            TotalCopies = 1,
            CategoryId = _category.Id,
            AuthorIds = [author.Id]
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await createResponse.AssertThatValidationErrorsContain("Title");
    }

    [Test]
    public async Task Update_ShouldReturnUpdatedBook()
    {
        var oldAuthor = await TestDataFactory.CreateAuthorAsync("Old", "Author");
        var newAuthor = await TestDataFactory.CreateAuthorAsync("New", "Author");
        var newCategory = await TestDataFactory.CreateCategoryAsync("History");
        var createdBook = await TestDataFactory.CreateBookAsync(
            [oldAuthor.Id], _category.Id, TitleDune, totalCopies: 2);

        var updateResponse = await PutAsync(new UpdateBookDto
        {
            Title = TitleEmma,
            TotalCopies = 2,
            CategoryId = newCategory.Id,
            AuthorIds = [newAuthor.Id]
        }, createdBook.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedBook = await updateResponse.ReadPayloadAsync<ReadBookDto>();

        Assert.That(updatedBook, Is.Not.Null);
        Assert.That(updatedBook.Id, Is.EqualTo(createdBook.Id));
        Assert.That(updatedBook.Title, Is.EqualTo(TitleEmma));
        Assert.That(updatedBook.CategoryId, Is.EqualTo(newCategory.Id));
        Assert.That(updatedBook.CategoryName, Is.EqualTo(newCategory.Name));
        Assert.That(updatedBook.Authors.Select(author => author.Id), Is.EqualTo([newAuthor.Id]));

        var fetchedBook = await GetBookAsync(createdBook.Id);
        Assert.That(fetchedBook.Title, Is.EqualTo(TitleEmma));
        Assert.That(fetchedBook.Authors.Select(author => author.Id), Is.EqualTo([newAuthor.Id]));
    }

    [Test]
    public async Task Update_ShouldIncreaseAvailableCopies_WhenTotalCopiesIncrease()
    {
        var book = await TestDataFactory.CreateBookAsync(
            _category.Id, TitleDune, totalCopies: 3);
        await TestDataFactory.CreateLoanForBookAsync(book.Id);

        var updateResponse = await PutAsync(ToUpdateDto(book, totalCopies: 5), book.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedBook = await updateResponse.ReadPayloadAsync<ReadBookDto>();

        Assert.That(updatedBook, Is.Not.Null);
        Assert.That(updatedBook.TotalCopies, Is.EqualTo(5));
        Assert.That(updatedBook.AvailableCopies, Is.EqualTo(4));
    }

    [Test]
    public async Task Update_ShouldDecreaseAvailableCopies_WhenTotalCopiesDecreaseDownToActiveLoansCount()
    {
        var book = await TestDataFactory.CreateBookAsync(
            _category.Id, TitleDune, totalCopies: 3);
        await TestDataFactory.CreateLoanAsync(book.Id, _member.Id);
        await TestDataFactory.CreateLoanAsync(book.Id, _member.Id);

        var updateResponse = await PutAsync(ToUpdateDto(book, totalCopies: 2), book.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedBook = await updateResponse.ReadPayloadAsync<ReadBookDto>();

        Assert.That(updatedBook, Is.Not.Null);
        Assert.That(updatedBook.TotalCopies, Is.EqualTo(2));
        Assert.That(updatedBook.AvailableCopies, Is.Zero);
    }

    [Test]
    public async Task Update_ShouldReturnConflict_WhenTotalCopiesDropBelowActiveLoansCount()
    {
        var book = await TestDataFactory.CreateBookAsync(
            _category.Id, TitleDune, totalCopies: 3);
        await TestDataFactory.CreateLoanAsync(book.Id, _member.Id);
        await TestDataFactory.CreateLoanAsync(book.Id, _member.Id);

        var updateResponse = await PutAsync(ToUpdateDto(book, totalCopies: 1), book.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await updateResponse.AssertThatProblemDetailsContain(book.Id.ToString());

        var fetchedBook = await GetBookAsync(book.Id);
        Assert.That(fetchedBook.TotalCopies, Is.EqualTo(3));
        Assert.That(fetchedBook.AvailableCopies, Is.EqualTo(1));
    }

    [Test]
    public async Task Update_ShouldAllowReducingTotalCopiesToZero_WhenAllLoansWereReturned()
    {
        var book = await TestDataFactory.CreateBookAsync(
            _category.Id, TitleDune, totalCopies: 2);
        await TestDataFactory.CreateReturnedLoanAsync(book.Id, _member.Id);
        await TestDataFactory.CreateReturnedLoanAsync(book.Id, _member.Id);

        var updateResponse = await PutAsync(ToUpdateDto(book, totalCopies: 0), book.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedBook = await updateResponse.ReadPayloadAsync<ReadBookDto>();

        Assert.That(updatedBook, Is.Not.Null);
        Assert.That(updatedBook.TotalCopies, Is.Zero);
        Assert.That(updatedBook.AvailableCopies, Is.Zero);
    }

    [Test]
    public async Task Update_ShouldReturnNotFound_WhenNoBookWithTheGivenIdExists()
    {
        var author = await TestDataFactory.CreateAuthorAsync();

        var updateResponse = await PutAsync(new UpdateBookDto
        {
            Title = TitleDune,
            TotalCopies = 1,
            CategoryId = _category.Id,
            AuthorIds = [author.Id]
        }, NonExistentId);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await updateResponse.AssertThatProblemDetailsContain(NonExistentId.ToString());
    }

    [Test]
    public async Task Update_ShouldReturnBadRequest_WhenCategoryDoesNotExist()
    {
        var book = await TestDataFactory.CreateBookAsync(_category.Id, TitleDune);

        var updateDto = ToUpdateDto(book, categoryId: NonExistentId);
        var updateResponse = await PutAsync(updateDto, book.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await updateResponse.AssertThatProblemDetailsContain(NonExistentId.ToString());
    }

    [Test]
    public async Task Update_ShouldReturnBadRequest_WhenAnyAuthorDoesNotExist()
    {
        var book = await TestDataFactory.CreateBookAsync(_category.Id, TitleDune);

        var updateDto = ToUpdateDto(book, authorIds: [book.Authors.First().Id, NonExistentId]);
        var updateResponse = await PutAsync(updateDto, book.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await updateResponse.AssertThatProblemDetailsContain("author");
    }

    [Test]
    public async Task Update_ShouldReturnBadRequest_WhenDtoIsInvalid()
    {
        var book = await TestDataFactory.CreateBookAsync(_category.Id, TitleDune);

        var updateDto = ToUpdateDto(book, title: string.Empty);
        var updateResponse = await PutAsync(updateDto, book.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await updateResponse.AssertThatValidationErrorsContain("Title");

        var fetchedBook = await GetBookAsync(book.Id);
        Assert.That(fetchedBook.Title, Is.EqualTo(TitleDune));
    }

    [Test]
    public async Task Delete_ShouldReturnNoContent_AndRemoveBook()
    {
        var book = await TestDataFactory.CreateBookAsync(_category.Id, TitleDune);

        var deleteResponse = await DeleteAsync(book.Id);
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResponse = await GetByIdAsync(book.Id);
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_ShouldReturnNotFound_WhenNoBookWithTheGivenIdExists()
    {
        var deleteResponse = await DeleteAsync(NonExistentId);

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await deleteResponse.AssertThatProblemDetailsContain(NonExistentId.ToString());
    }

    [Test]
    public async Task Delete_ShouldReturnConflict_WhenBookHasActiveLoans()
    {
        var book = await TestDataFactory.CreateBookAsync(
            _category.Id, TitleDune, totalCopies: 1);
        await TestDataFactory.CreateLoanForBookAsync(book.Id);

        var deleteResponse = await DeleteAsync(book.Id);

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await deleteResponse.AssertThatProblemDetailsContain(book.Id.ToString());

        var getResponse = await GetByIdAsync(book.Id);
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Delete_ShouldReturnConflict_WhenBookHasLoanHistory_EvenIfAllLoansWereReturned()
    {
        var book = await TestDataFactory.CreateBookAsync(
            _category.Id, TitleDune, totalCopies: 1);
        await TestDataFactory.CreateReturnedLoanForBookAsync(book.Id);

        var deleteResponse = await DeleteAsync(book.Id);

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await deleteResponse.AssertThatProblemDetailsContain(book.Id.ToString());
    }

    private async Task<ReadBookDto> GetBookAsync(int id)
    {
        var getResponse = await GetByIdAsync(id);
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        return (await getResponse.ReadPayloadAsync<ReadBookDto>())!;
    }

    private static UpdateBookDto ToUpdateDto(
        ReadBookDto book,
        string? title = null,
        int? totalCopies = null,
        int? categoryId = null,
        int[]? authorIds = null)
    {
        return new UpdateBookDto
        {
            Title = title ?? book.Title,
            TotalCopies = totalCopies ?? book.TotalCopies,
            CategoryId = categoryId ?? book.CategoryId,
            AuthorIds = authorIds ?? book.Authors.Select(author => author.Id).ToArray()
        };
    }
}
