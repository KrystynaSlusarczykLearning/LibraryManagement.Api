using LibraryManagement.Api.DTOs.Authors;
using LibraryManagement.Api.DTOs.Books;
using LibraryManagement.Api.DTOs.Categories;
using LibraryManagement.Api.DTOs.Loans;
using LibraryManagement.Api.DTOs.Members;
using System.Net.Http.Json;

namespace LibraryManagement.IntegrationTests.Utils;

internal class TestDataFactory
{
    private const string DefaultCategoryName = "Fiction";
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

        return createdAuthor!;
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

        return createdBook!;
    }

    public async Task<ReadBookDto> CreateBookAsync(
        int categoryId,
        string title = "Some Title",
        int totalCopies = 3)
    {
        var author = await CreateAuthorAsync();

        return await CreateBookAsync(
           [author.Id],
           categoryId,
           title,
           totalCopies);
    }

    public async Task<ReadBookDto> CreateBookAsync(
        int[] authorIds,
        string title = "Some Title",
        int totalCopies = 3)
    {
        var category = await GetOrCreateDefaultCategoryAsync();

        return await CreateBookAsync(
            authorIds,
            category.Id,
            title,
            totalCopies);
    }

    public async Task<ReadBookDto> CreateBookAsync(
        string title = "Some Title",
        int totalCopies = 3)
    {
        var category = await GetOrCreateDefaultCategoryAsync();

        return await CreateBookAsync(
            category.Id,
            title,
            totalCopies);
    }

    public async Task<ReadCategoryDto> CreateCategoryAsync(
        string name = DefaultCategoryName)
    {
        var createCategoryDto = new CreateCategoryDto { Name = name };

        var createCategoryResponse = await _client.PostAsJsonAsync(
            "/api/categories", createCategoryDto);

        await createCategoryResponse.EnsureSuccessStatusCodeWitDetailsAsync();

        var createdCategory =
            await createCategoryResponse.ReadPayloadAsync<ReadCategoryDto>();

        return createdCategory!;
    }

    private async Task<ReadCategoryDto> GetOrCreateDefaultCategoryAsync()
    {
        var categories = await _client.GetFromJsonAsync<List<ReadCategoryDto>>(
            "/api/categories");

        return categories!.FirstOrDefault(category => category.Name == DefaultCategoryName)
            ?? await CreateCategoryAsync(DefaultCategoryName);
    }

    public async Task<ReadMemberDto> CreateMemberAsync(
        string firstName = "Jane",
        string lastName = "Smith")
    {
        var createMemberDto = new CreateMemberDto
        {
            FirstName = firstName,
            LastName = lastName
        };

        var createMemberResponse = await _client.PostAsJsonAsync(
            "/api/members", createMemberDto);

        await createMemberResponse.EnsureSuccessStatusCodeWitDetailsAsync();

        var createdMember =
            await createMemberResponse.ReadPayloadAsync<ReadMemberDto>();

        return createdMember!;
    }

    public async Task<ReadMemberDto> CreateInactiveMemberAsync(
        string firstName = "Jane",
        string lastName = "Smith")
    {
        var member = await CreateMemberAsync(firstName, lastName);

        var deleteMemberResponse = await _client.DeleteAsync(
            $"/api/members/{member.Id}");

        await deleteMemberResponse.EnsureSuccessStatusCodeWitDetailsAsync();

        return member with { IsActive = false };
    }

    public async Task<ReadLoanDto> CreateLoanAsync(
        int bookId,
        int memberId,
        DateOnly? loanDate = null,
        DateOnly? dueDate = null)
    {
        var effectiveLoanDate = loanDate ?? DateOnly.FromDateTime(DateTime.Today);

        var createLoanDto = new CreateLoanDto
        {
            BookId = bookId,
            MemberId = memberId,
            LoanDate = effectiveLoanDate,
            DueDate = dueDate ?? effectiveLoanDate.AddDays(14)
        };

        var createLoanResponse = await _client.PostAsJsonAsync(
            "/api/loans", createLoanDto);

        await createLoanResponse.EnsureSuccessStatusCodeWitDetailsAsync();

        var createdLoan =
            await createLoanResponse.ReadPayloadAsync<ReadLoanDto>();

        return createdLoan!;
    }

    public async Task<ReadLoanDto> CreateLoanForBookAsync(int bookId)
    {
        var member = await CreateMemberAsync();

        return await CreateLoanAsync(bookId, member.Id);
    }

    public async Task<ReadLoanDto> CreateLoanForMemberAsync(int memberId)
    {
        var book = await CreateBookAsync();

        return await CreateLoanAsync(book.Id, memberId);
    }

    public async Task<ReadLoanDto> CreateReturnedLoanAsync(
        int bookId,
        int memberId)
    {
        var loan = await CreateLoanAsync(bookId, memberId);

        var returnLoanResponse = await _client.PatchAsync(
            $"/api/loans/{loan.Id}/return", null);

        await returnLoanResponse.EnsureSuccessStatusCodeWitDetailsAsync();

        var returnedLoan =
            await returnLoanResponse.ReadPayloadAsync<ReadLoanDto>();

        return returnedLoan!;
    }

    public async Task<ReadLoanDto> CreateReturnedLoanForBookAsync(int bookId)
    {
        var member = await CreateMemberAsync();

        return await CreateReturnedLoanAsync(bookId, member.Id);
    }

    public async Task<ReadLoanDto> CreateReturnedLoanForMemberAsync(int memberId)
    {
        var book = await CreateBookAsync();

        return await CreateReturnedLoanAsync(book.Id, memberId);
    }
}
