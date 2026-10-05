using LibraryManagement.Api.DTOs.Books;
using LibraryManagement.Api.DTOs.Loans;
using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.Entities;
using LibraryManagement.IntegrationTests.Utils;
using System.Net;

namespace LibraryManagement.IntegrationTests;

[TestFixture]
internal class LoansTests : IntegrationTestsBase
{
    protected override string DatabaseName => nameof(LoansTests);
    protected override string BaseUrl => "/api/loans";
    private const string BookTitle = "Dune";
    private const string SampleEmail = "email1@gmail.com";
    private const int BookTotalCopies = 10;
    private const int NonExistentId = 99;
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    private ReadBookDto _book = null!;
    private ReadMemberDto _member = null!;

    [SetUp]
    public async Task CreateBookAndMember()
    {
        _book = await TestDataFactory.CreateBookAsync(BookTitle, BookTotalCopies);
        _member = await TestDataFactory.CreateMemberAsync();
    }

    [Test]
    public async Task GetById_ShouldReturnLoan_WithBookAndMemberDetails()
    {
        var createdLoan = await TestDataFactory.CreateLoanAsync(
            _book.Id, _member.Id, Today, Today.AddDays(14));

        var getResponse = await GetByIdAsync(createdLoan.Id);

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var fetchedLoan = await getResponse.ReadPayloadAsync<ReadLoanDto>();

        Assert.That(fetchedLoan, Is.EqualTo(new ReadLoanDto
        {
            Id = createdLoan.Id,
            BookId = _book.Id,
            BookTitle = BookTitle,
            MemberId = _member.Id,
            MemberFirstName = _member.FirstName,
            MemberLastName = _member.LastName,
            LoanDate = Today,
            DueDate = Today.AddDays(14),
            ReturnDate = null,
            Status = LoanStatus.Active
        }));
    }

    [Test]
    public async Task GetById_ShouldReturnNotFound_WhenTheLoanDoesNotExist()
    {
        var getResponse = await GetByIdAsync(NonExistentId);

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await getResponse.AssertThatProblemDetailsContain(NonExistentId.ToString());
    }

    [Test]
    public async Task GetAll_ShouldReturnAllLoans_OrderedById_ByDefault()
    {
        var loan1 = await TestDataFactory.CreateLoanAsync(
            _book.Id, _member.Id, loanDate: Today);
        var loan2 = await TestDataFactory.CreateLoanAsync(
            _book.Id, _member.Id, loanDate: Today.AddDays(-5));

        var response = await GetAllAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var loans = await response.ReadPayloadAsync<List<ReadLoanDto>>();

        Assert.That(loans, Is.Not.Null);
        Assert.That(loans, Is.EqualTo([loan1, loan2]));

        response.AssertThatPaginationHeadersAre(totalCount: 2, totalPages: 1);
    }

    [Test]
    public async Task GetAll_ShouldReturnEmptyCollection_WhenNoLoansExist()
    {
        var response = await GetAllAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var loans = await response.ReadPayloadAsync<List<ReadLoanDto>>();

        Assert.That(loans, Is.Not.Null);
        Assert.That(loans, Is.Empty);

        response.AssertThatPaginationHeadersAre(totalCount: 0, totalPages: 0);
    }

    [Test]
    public async Task GetAll_ShouldRespectPagination_AndSetResponseHeaders()
    {
        var loanIds = new List<int>();
        for (int i = 1; i <= 5; ++i)
        {
            var loan = await TestDataFactory.CreateLoanAsync(_book.Id, _member.Id);
            loanIds.Add(loan.Id);
        }

        var response = await GetWithQuery(BaseUrl, new()
        {
            ["pageNumber"] = "2",
            ["pageSize"] = "3"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var loans = await response.ReadPayloadAsync<List<ReadLoanDto>>();

        Assert.That(loans, Is.Not.Null);
        Assert.That(loans.Select(loan => loan.Id), Is.EqualTo(loanIds[3..]));

        response.AssertThatPaginationHeadersAre(totalCount: 5, totalPages: 2);
    }

    [Test]
    public async Task GetAll_ShouldFilterByMemberId()
    {
        var otherMember = await TestDataFactory.CreateMemberAsync(
            "John", "Brown", SampleEmail);
        var loan = await TestDataFactory.CreateLoanAsync(_book.Id, _member.Id);
        await TestDataFactory.CreateLoanAsync(_book.Id, otherMember.Id);

        var response = await GetWithQuery(BaseUrl,
            new() { ["memberId"] = _member.Id.ToString() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var loans = await response.ReadPayloadAsync<List<ReadLoanDto>>();

        Assert.That(loans, Is.Not.Null);
        Assert.That(loans.Select(loan => loan.Id), Is.EqualTo([loan.Id]));
    }

    [Test]
    public async Task GetAll_ShouldFilterByBookId()
    {
        var otherBook = await TestDataFactory.CreateBookAsync(
            _book.CategoryId, "Emma");
        await TestDataFactory.CreateLoanAsync(_book.Id, _member.Id);
        var loan = await TestDataFactory.CreateLoanAsync(otherBook.Id, _member.Id);

        var response = await GetWithQuery(BaseUrl,
            new() { ["bookId"] = otherBook.Id.ToString() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var loans = await response.ReadPayloadAsync<List<ReadLoanDto>>();

        Assert.That(loans, Is.Not.Null);
        Assert.That(loans.Select(loan => loan.Id), Is.EqualTo([loan.Id]));
    }

    [TestCase(LoanStatus.Active)]
    [TestCase(LoanStatus.Overdue)]
    [TestCase(LoanStatus.Returned)]
    public async Task GetAll_ShouldFilterByLoanStatus(LoanStatus loanStatus)
    {
        var activeLoan = await TestDataFactory.CreateLoanAsync(
            _book.Id, _member.Id, Today.AddDays(-3), Today.AddDays(11));
        var overdueLoan = await TestDataFactory.CreateLoanAsync(
            _book.Id, _member.Id, Today.AddDays(-20), Today.AddDays(-6));
        var returnedLoan = await TestDataFactory.CreateReturnedLoanAsync(
            _book.Id, _member.Id);

        var loanIdsByStatus = new Dictionary<LoanStatus, int>
        {
            [LoanStatus.Active] = activeLoan.Id,
            [LoanStatus.Overdue] = overdueLoan.Id,
            [LoanStatus.Returned] = returnedLoan.Id
        };

        var response = await GetWithQuery(BaseUrl,
            new() { ["loanStatus"] = loanStatus.ToString() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var loans = await response.ReadPayloadAsync<List<ReadLoanDto>>();

        Assert.That(loans, Is.Not.Null);
        Assert.That(loans.Select(loan => loan.Id), Is.EqualTo([loanIdsByStatus[loanStatus]]));
        Assert.That(loans.Select(loan => loan.Status), Is.EqualTo([loanStatus]));
    }

    [TestCase(nameof(ReadLoanDto.LoanDate), "Asc")]
    [TestCase(nameof(ReadLoanDto.LoanDate), "Desc")]
    [TestCase(nameof(ReadLoanDto.DueDate), "Asc")]
    [TestCase(nameof(ReadLoanDto.DueDate), "Desc")]
    public async Task GetAll_ShouldSortBy(string sortBy, string sortDirection)
    {
        await TestDataFactory.CreateLoanAsync(
            _book.Id, _member.Id, Today, Today.AddDays(10));
        await TestDataFactory.CreateLoanAsync(
            _book.Id, _member.Id, Today.AddDays(-10), Today.AddDays(20));
        await TestDataFactory.CreateLoanAsync(
            _book.Id, _member.Id, Today.AddDays(-5), Today.AddDays(5));

        var response = await GetWithQuery(BaseUrl, new()
        {
            ["sortBy"] = sortBy,
            ["sortDirection"] = sortDirection
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var loans = await response.ReadPayloadAsync<List<ReadLoanDto>>();

        Assert.That(loans, Is.Not.Null);
        Assert.That(loans, Has.Count.EqualTo(3));
        Assert.That(loans, sortDirection == "Asc"
            ? Is.Ordered.Ascending.By(sortBy)
            : Is.Ordered.Descending.By(sortBy));
    }

    [Test]
    public async Task GetAll_ShouldReturnBadRequest_WhenLoanStatusIsInvalid()
    {
        var response = await GetWithQuery(BaseUrl, new() { ["loanStatus"] = "Lost" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await response.AssertThatValidationErrorsContain("LoanStatus");
    }

    [Test]
    public async Task Create_ShouldReturnCreated_AndDecreaseAvailableCopies()
    {
        var createDto = new CreateLoanDto
        {
            BookId = _book.Id,
            MemberId = _member.Id,
            LoanDate = Today,
            DueDate = Today.AddDays(14)
        };

        var createResponse = await PostAsync(createDto);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var createdLoan = await createResponse.ReadPayloadAsync<ReadLoanDto>();

        Assert.That(createdLoan, Is.Not.Null);
        Assert.That(createdLoan.Id, Is.GreaterThan(0));
        Assert.That(createdLoan.BookId, Is.EqualTo(_book.Id));
        Assert.That(createdLoan.BookTitle, Is.EqualTo(BookTitle));
        Assert.That(createdLoan.MemberId, Is.EqualTo(_member.Id));
        Assert.That(createdLoan.LoanDate, Is.EqualTo(Today));
        Assert.That(createdLoan.DueDate, Is.EqualTo(Today.AddDays(14)));
        Assert.That(createdLoan.ReturnDate, Is.Null);
        Assert.That(createdLoan.Status, Is.EqualTo(LoanStatus.Active));

        Assert.That(createResponse.GetLocation(),
            Is.EqualTo($"{BaseUrl}/{createdLoan.Id}"));

        await AssertAvailableCopies(_book.Id, BookTotalCopies - 1);
    }

    [Test]
    public async Task Create_ShouldReturnCreated_WhenLoanDateEqualsDueDate()
    {
        var createResponse = await PostAsync(new CreateLoanDto
        {
            BookId = _book.Id,
            MemberId = _member.Id,
            LoanDate = Today,
            DueDate = Today
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task Create_ShouldReturnBadRequest_WhenLoanDateIsAfterDueDate()
    {
        var createResponse = await PostAsync(new CreateLoanDto
        {
            BookId = _book.Id,
            MemberId = _member.Id,
            LoanDate = Today.AddDays(1),
            DueDate = Today
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await createResponse.AssertThatProblemDetailsContain("Due date");

        await AssertAvailableCopies(_book.Id, BookTotalCopies);
    }

    [Test]
    public async Task Create_ShouldReturnBadRequest_WhenBookDoesNotExist()
    {
        var createResponse = await PostAsync(new CreateLoanDto
        {
            BookId = NonExistentId,
            MemberId = _member.Id,
            LoanDate = Today,
            DueDate = Today.AddDays(14)
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await createResponse.AssertThatProblemDetailsContain(
            $"book with Id {NonExistentId}");
    }

    [Test]
    public async Task Create_ShouldReturnBadRequest_WhenMemberDoesNotExist()
    {
        var createResponse = await PostAsync(new CreateLoanDto
        {
            BookId = _book.Id,
            MemberId = NonExistentId,
            LoanDate = Today,
            DueDate = Today.AddDays(14)
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await createResponse.AssertThatProblemDetailsContain(
            $"member with Id {NonExistentId}");

        await AssertAvailableCopies(_book.Id, BookTotalCopies);
    }

    [Test]
    public async Task Create_ShouldReturnConflict_WhenBookHasNoAvailableCopies()
    {
        var book = await TestDataFactory.CreateBookAsync(
            _book.CategoryId, "Emma", totalCopies: 1);
        await TestDataFactory.CreateLoanAsync(book.Id, _member.Id);

        var createResponse = await PostAsync(new CreateLoanDto
        {
            BookId = book.Id,
            MemberId = _member.Id,
            LoanDate = Today,
            DueDate = Today.AddDays(14)
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await createResponse.AssertThatProblemDetailsContain(book.Id.ToString());

        await AssertAvailableCopies(book.Id, 0);
    }

    [Test]
    public async Task Create_ShouldReturnConflict_WhenMemberIsInactive()
    {
        var inactiveMember = await TestDataFactory.CreateInactiveMemberAsync(
            "John", "Brown", SampleEmail);

        var createResponse = await PostAsync(new CreateLoanDto
        {
            BookId = _book.Id,
            MemberId = inactiveMember.Id,
            LoanDate = Today,
            DueDate = Today.AddDays(14)
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await createResponse.AssertThatProblemDetailsContain(inactiveMember.Id.ToString());

        await AssertAvailableCopies(_book.Id, BookTotalCopies);
    }

    [Test]
    public async Task Return_ShouldReturnReturnedLoan_AndIncreaseAvailableCopies()
    {
        var loan = await TestDataFactory.CreateLoanAsync(_book.Id, _member.Id);

        var returnResponse = await ReturnAsync(loan.Id);

        Assert.That(returnResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var returnedLoan = await returnResponse.ReadPayloadAsync<ReadLoanDto>();

        Assert.That(returnedLoan, Is.EqualTo(loan with
        {
            ReturnDate = Today,
            Status = LoanStatus.Returned
        }));

        await AssertAvailableCopies(_book.Id, BookTotalCopies);
    }

    [Test]
    public async Task Return_ShouldReturnNotFound_WhenNoLoanWithTheGivenIdExists()
    {
        var returnResponse = await ReturnAsync(NonExistentId);

        Assert.That(returnResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await returnResponse.AssertThatProblemDetailsContain(NonExistentId.ToString());
    }

    [Test]
    public async Task Return_ShouldReturnConflict_WhenLoanIsAlreadyReturned()
    {
        var loan = await TestDataFactory.CreateReturnedLoanAsync(_book.Id, _member.Id);

        var returnResponse = await ReturnAsync(loan.Id);

        Assert.That(returnResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await returnResponse.AssertThatProblemDetailsContain(loan.Id.ToString());

        await AssertAvailableCopies(_book.Id, BookTotalCopies);
    }

    private async Task<HttpResponseMessage> ReturnAsync(int id)
    {
        return await PatchWithoutBodyAsync($"{BaseUrl}/{id}/return");
    }

    private async Task AssertAvailableCopies(int bookId, int expectedAvailableCopies)
    {
        var getBookResponse = await Client.GetAsync($"/api/books/{bookId}");
        Assert.That(getBookResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var book = await getBookResponse.ReadPayloadAsync<ReadBookDto>();
        Assert.That(book, Is.Not.Null);
        Assert.That(book.AvailableCopies, Is.EqualTo(expectedAvailableCopies));
    }
}
