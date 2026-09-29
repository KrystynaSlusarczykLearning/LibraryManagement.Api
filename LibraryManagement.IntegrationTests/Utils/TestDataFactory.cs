using LibraryManagement.Api.DTOs.Authors;
using LibraryManagement.Api.DTOs.Books;
using System.Net.Http.Json;

namespace LibraryManagement.IntegrationTests.Utils;

internal class TestDataFactory
{
    private readonly HttpClient _client;

    public TestDataFactory(HttpClient client)
    {
        _client = client;
    }

    public async Task<ReadAuthorDetailDto> CreateAuthorAsync(
        string firstName = "John",
        string lastName = "Doe",
        int yearOfBirth = 1980)
    {
        var createAuthorDto = new CreateAuthorDto
        {
            FirstName = firstName,
            LastName = lastName,
            YearOfBirth = yearOfBirth
        };

        var createAuthorResponse = await _client.PostAsJsonAsync(
            "/api/authors", createAuthorDto);
        await createAuthorResponse.EnsureSuccessStatusCodeWitDetailsAsync();

        var createdAuthor = 
            await createAuthorResponse.ReadPayloadAsync<ReadAuthorDetailDto>();

        return createdAuthor;
    }

    public async Task<ReadBookDto> CreateBookAsync(
        int[] authorIds,
        int categoryId,
        string title = "Some Title",
        int totalCopies = 3)
    {
        var createBookDto = new CreateBookDto
        {
            Title = title,
            TotalCopies = totalCopies,
            AuthorIds = authorIds,
            CategoryId = categoryId
        };

        var createBookResponse = await _client.PostAsJsonAsync(
            "/api/books", createBookDto);

        await createBookResponse.EnsureSuccessStatusCodeWitDetailsAsync();

        var createdBook =
            await createBookResponse.ReadPayloadAsync<ReadBookDto>();

        return createdBook;
    }

    public async Task<ReadBookDto> CreateBookAsync(
        int categoryId,
        string title = "Some Title",
        int totalCopies = 3)
    {
        var author = await CreateAuthorAsync();

        return await CreateBookAsync(
           [author!.Id],
           categoryId,
           title, 
           totalCopies);      
    }
}
