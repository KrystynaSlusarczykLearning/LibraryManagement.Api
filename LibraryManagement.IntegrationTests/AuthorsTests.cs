using LibraryManagement.Api.DTOs.Authors;
using LibraryManagement.IntegrationTests.Utils;
using System.Net;

namespace LibraryManagement.IntegrationTests;

[TestFixture]
internal class AuthorsTests : IntegrationTestsBase
{
    protected override string DatabaseName => nameof(AuthorsTests);
    protected override string BaseUrl => "/api/authors";
    private const string FirstNameGeorge = "George";
    private const string LastNameOrwell = "Orwell";
    private const int YearOfBirthOrwell = 1903;
    private const string FirstNameJane = "Jane";
    private const string LastNameAusten = "Austen";
    private const int YearOfBirthAusten = 1775;
    private const int NonExistentAuthorId = 99;

    [Test]
    public async Task GetById_ShouldReturnAuthor_WithBooks()
    {
        var author = await TestDataFactory.CreateAuthorAsync(
            FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);
        var book1 = await TestDataFactory.CreateBookAsync([author.Id], "1984");
        var book2 = await TestDataFactory.CreateBookAsync([author.Id], "Animal Farm");

        var getResponse = await GetByIdAsync(author.Id);

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var fetchedAuthor = await getResponse.ReadPayloadAsync<ReadAuthorDetailDto>();

        Assert.That(fetchedAuthor, Is.Not.Null);
        Assert.That(fetchedAuthor.Id, Is.EqualTo(author.Id));
        Assert.That(fetchedAuthor.FirstName, Is.EqualTo(FirstNameGeorge));
        Assert.That(fetchedAuthor.LastName, Is.EqualTo(LastNameOrwell));
        Assert.That(fetchedAuthor.YearOfBirth, Is.EqualTo(YearOfBirthOrwell));
        Assert.That(
            fetchedAuthor.Books.Select(book => (book.Id, book.Title)),
            Is.EquivalentTo([(book1.Id, "1984"), (book2.Id, "Animal Farm")]));
    }

    [Test]
    public async Task GetById_ShouldReturnNotFound_WhenTheAuthorDoesNotExist()
    {
        var getResponse = await GetByIdAsync(NonExistentAuthorId);

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await getResponse.AssertThatProblemDetailsContain(
            NonExistentAuthorId.ToString());
    }

    [Test]
    public async Task GetAll_ShouldReturnAllAuthors_OrderedById()
    {
        var author1 = await TestDataFactory.CreateAuthorAsync(
            FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);
        var author2 = await TestDataFactory.CreateAuthorAsync(
            FirstNameJane, LastNameAusten, YearOfBirthAusten);

        var response = await GetAllAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var authors = await response.ReadPayloadAsync<List<ReadAuthorDto>>();

        Assert.That(authors, Is.Not.Null);
        Assert.That(authors, Is.EqualTo(new[]
        {
            new ReadAuthorDto
            {
                Id = author1.Id,
                FirstName = FirstNameGeorge,
                LastName = LastNameOrwell,
                YearOfBirth = YearOfBirthOrwell
            },
            new ReadAuthorDto
            {
                Id = author2.Id,
                FirstName = FirstNameJane,
                LastName = LastNameAusten,
                YearOfBirth = YearOfBirthAusten
            }
        }));

        response.AssertThatPaginationHeadersAre(totalCount: 2, totalPages: 1);
    }

    [Test]
    public async Task GetAll_ShouldReturnEmptyCollection_WhenNoAuthorsExist()
    {
        var response = await GetAllAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var authors = await response.ReadPayloadAsync<List<ReadAuthorDto>>();

        Assert.That(authors, Is.Not.Null);
        Assert.That(authors, Is.Empty);

        response.AssertThatPaginationHeadersAre(totalCount: 0, totalPages: 0);
    }

    [Test]
    public async Task GetAll_ShouldRespectPagination_AndSetResponseHeaders()
    {
        for (int i = 1; i <= 5; ++i)
        {
            await TestDataFactory.CreateAuthorAsync(lastName: $"Author{i}");
        }

        var response = await GetWithQuery(BaseUrl, new()
        {
            ["pageNumber"] = "2",
            ["pageSize"] = "3"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var authors = await response.ReadPayloadAsync<List<ReadAuthorDto>>();

        Assert.That(authors, Is.Not.Null);
        Assert.That(
            authors.Select(author => author.LastName),
            Is.EqualTo(["Author4", "Author5"]));

        response.AssertThatPaginationHeadersAre(totalCount: 5, totalPages: 2);
    }

    [Test]
    public async Task Create_ShouldReturnCreated_WithCorrectData()
    {
        var createDto = new CreateAuthorDto
        {
            FirstName = FirstNameGeorge,
            LastName = LastNameOrwell,
            YearOfBirth = YearOfBirthOrwell
        };

        var createResponse = await PostAsync(createDto);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var createdAuthor = await createResponse.ReadPayloadAsync<ReadAuthorDetailDto>();

        Assert.That(createdAuthor, Is.Not.Null);
        Assert.That(createdAuthor.Id, Is.GreaterThan(0));
        Assert.That(createdAuthor.FirstName, Is.EqualTo(FirstNameGeorge));
        Assert.That(createdAuthor.LastName, Is.EqualTo(LastNameOrwell));
        Assert.That(createdAuthor.YearOfBirth, Is.EqualTo(YearOfBirthOrwell));
        Assert.That(createdAuthor.Books, Is.Empty);

        Assert.That(createResponse.GetLocation(),
            Is.EqualTo($"{BaseUrl}/{createdAuthor.Id}"));

        await AssertAuthorExistsInTheDatabase(
            createdAuthor.Id, FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);
    }

    [Test]
    public async Task Create_ShouldReturnCreated_WhenYearOfBirthIsNotProvided()
    {
        var createDto = new CreateAuthorDto
        {
            FirstName = FirstNameGeorge,
            LastName = LastNameOrwell
        };

        var createResponse = await PostAsync(createDto);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var createdAuthor = await createResponse.ReadPayloadAsync<ReadAuthorDetailDto>();

        Assert.That(createdAuthor, Is.Not.Null);
        Assert.That(createdAuthor.YearOfBirth, Is.Null);
    }

    [Test]
    public async Task Create_ShouldReturnBadRequest_WhenDtoIsInvalid()
    {
        var createDto = new CreateAuthorDto
        {
            FirstName = string.Empty,
            LastName = LastNameOrwell
        };

        var createResponse = await PostAsync(createDto);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await createResponse.AssertThatValidationErrorsContain("FirstName");
    }

    [Test]
    public async Task Update_ShouldReturnUpdatedAuthor_AndKeepBooks()
    {
        var author = await TestDataFactory.CreateAuthorAsync(
            FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);
        var book = await TestDataFactory.CreateBookAsync([author.Id], "1984");

        var updateDto = new UpdateAuthorDto
        {
            FirstName = FirstNameJane,
            LastName = LastNameAusten,
            YearOfBirth = YearOfBirthAusten
        };
        var updateResponse = await PutAsync(updateDto, author.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedAuthor = await updateResponse.ReadPayloadAsync<ReadAuthorDetailDto>();

        Assert.That(updatedAuthor, Is.Not.Null);
        Assert.That(updatedAuthor.Id, Is.EqualTo(author.Id));
        Assert.That(updatedAuthor.FirstName, Is.EqualTo(FirstNameJane));
        Assert.That(updatedAuthor.LastName, Is.EqualTo(LastNameAusten));
        Assert.That(updatedAuthor.YearOfBirth, Is.EqualTo(YearOfBirthAusten));
        Assert.That(updatedAuthor.Books.Select(book => book.Id), Is.EqualTo([book.Id]));

        await AssertAuthorExistsInTheDatabase(
            author.Id, FirstNameJane, LastNameAusten, YearOfBirthAusten);
    }

    [Test]
    public async Task Update_ShouldClearYearOfBirth_WhenItIsNotProvided()
    {
        var author = await TestDataFactory.CreateAuthorAsync(
            FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);

        var updateDto = new UpdateAuthorDto
        {
            FirstName = FirstNameGeorge,
            LastName = LastNameOrwell
        };
        var updateResponse = await PutAsync(updateDto, author.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        await AssertAuthorExistsInTheDatabase(
            author.Id, FirstNameGeorge, LastNameOrwell, yearOfBirth: null);
    }

    [Test]
    public async Task Update_ShouldReturnNotFound_WhenNoAuthorWithTheGivenIdExists()
    {
        var updateDto = new UpdateAuthorDto
        {
            FirstName = FirstNameJane,
            LastName = LastNameAusten
        };
        var updateResponse = await PutAsync(updateDto, NonExistentAuthorId);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await updateResponse.AssertThatProblemDetailsContain(
            NonExistentAuthorId.ToString());
    }

    [Test]
    public async Task Update_ShouldReturnBadRequest_WhenDtoIsInvalid()
    {
        var author = await TestDataFactory.CreateAuthorAsync(
            FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);

        var updateDto = new UpdateAuthorDto
        {
            FirstName = string.Empty,
            LastName = LastNameAusten
        };
        var updateResponse = await PutAsync(updateDto, author.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await updateResponse.AssertThatValidationErrorsContain("FirstName");

        await AssertAuthorExistsInTheDatabase(
            author.Id, FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);
    }

    [Test]
    public async Task Patch_ShouldUpdateOnlyPatchedProperties()
    {
        var author = await TestDataFactory.CreateAuthorAsync(
            FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);

        var patchResponse = await JsonPatchAsync(
            author.Id,
            JsonPatchOperation.Replace("/lastName", LastNameAusten));

        Assert.That(patchResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var patchedAuthor = await patchResponse.ReadPayloadAsync<ReadAuthorDetailDto>();

        Assert.That(patchedAuthor, Is.Not.Null);
        Assert.That(patchedAuthor.FirstName, Is.EqualTo(FirstNameGeorge));
        Assert.That(patchedAuthor.LastName, Is.EqualTo(LastNameAusten));
        Assert.That(patchedAuthor.YearOfBirth, Is.EqualTo(YearOfBirthOrwell));

        await AssertAuthorExistsInTheDatabase(
            author.Id, FirstNameGeorge, LastNameAusten, YearOfBirthOrwell);
    }

    [Test]
    public async Task Patch_ShouldApplyMultipleOperations()
    {
        var author = await TestDataFactory.CreateAuthorAsync(
            FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);

        var patchResponse = await JsonPatchAsync(
            author.Id,
            JsonPatchOperation.Replace("/firstName", FirstNameJane),
            JsonPatchOperation.Replace("/yearOfBirth", YearOfBirthAusten));

        Assert.That(patchResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        await AssertAuthorExistsInTheDatabase(
            author.Id, FirstNameJane, LastNameOrwell, YearOfBirthAusten);
    }

    [Test]
    public async Task Patch_ShouldReturnNotFound_WhenNoAuthorWithTheGivenIdExists()
    {
        var patchResponse = await JsonPatchAsync(
            NonExistentAuthorId,
            JsonPatchOperation.Replace("/lastName", LastNameAusten));

        Assert.That(patchResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await patchResponse.AssertThatProblemDetailsContain(
            NonExistentAuthorId.ToString());
    }

    [Test]
    public async Task Patch_ShouldReturnBadRequest_WhenPatchedValueIsInvalid()
    {
        var author = await TestDataFactory.CreateAuthorAsync(
            FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);

        var patchResponse = await JsonPatchAsync(
            author.Id,
            JsonPatchOperation.Replace("/firstName", string.Empty));

        Assert.That(patchResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await patchResponse.AssertThatValidationErrorsContain("FirstName");

        await AssertAuthorExistsInTheDatabase(
            author.Id, FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);
    }

    [Test]
    public async Task Patch_ShouldReturnBadRequest_WhenPathDoesNotExist()
    {
        var author = await TestDataFactory.CreateAuthorAsync(
            FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);

        var patchResponse = await JsonPatchAsync(
            author.Id,
            JsonPatchOperation.Replace("/nonExistentProperty", "value"));

        Assert.That(patchResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        await AssertAuthorExistsInTheDatabase(
            author.Id, FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);
    }

    [Test]
    public async Task Delete_ShouldReturnNoContent_AndRemoveAuthor()
    {
        var author = await TestDataFactory.CreateAuthorAsync();

        var deleteResponse = await DeleteAsync(author.Id);
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResponse = await GetByIdAsync(author.Id);
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_ShouldReturnNotFound_WhenNoAuthorWithTheGivenIdExists()
    {
        var deleteResponse = await DeleteAsync(NonExistentAuthorId);

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await deleteResponse.AssertThatProblemDetailsContain(
            NonExistentAuthorId.ToString());
    }

    [Test]
    public async Task Delete_ShouldReturnConflict_WhenAuthorHasBooks()
    {
        var author = await TestDataFactory.CreateAuthorAsync(
            FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);
        await TestDataFactory.CreateBookAsync([author.Id]);

        var deleteResponse = await DeleteAsync(author.Id);

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await deleteResponse.AssertThatProblemDetailsContain(author.Id.ToString());

        await AssertAuthorExistsInTheDatabase(
            author.Id, FirstNameGeorge, LastNameOrwell, YearOfBirthOrwell);
    }

    private async Task AssertAuthorExistsInTheDatabase(
        int id, string firstName, string lastName, int? yearOfBirth)
    {
        var getAuthorResponse = await GetByIdAsync(id);
        Assert.That(getAuthorResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var authorFromGetResponse =
            await getAuthorResponse.ReadPayloadAsync<ReadAuthorDetailDto>();
        Assert.That(authorFromGetResponse, Is.Not.Null);
        Assert.That(authorFromGetResponse.FirstName, Is.EqualTo(firstName));
        Assert.That(authorFromGetResponse.LastName, Is.EqualTo(lastName));
        Assert.That(authorFromGetResponse.YearOfBirth, Is.EqualTo(yearOfBirth));
    }
}
