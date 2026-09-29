using LibraryManagement.Api.DTOs.Loans;
using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.Entities;
using LibraryManagement.IntegrationTests.Utils;
using System.Net;

namespace LibraryManagement.IntegrationTests;

[TestFixture]
internal class MembersTests : IntegrationTestsBase
{
    protected override string DatabaseName => nameof(MembersTests);
    protected override string BaseUrl => "/api/members";
    private const string FirstNameJane = "Jane";
    private const string LastNameSmith = "Smith";
    private const string FirstNameJohn = "John";
    private const string LastNameBrown = "Brown";
    private const int NonExistentMemberId = 99;

    [Test]
    public async Task GetById_ShouldReturnMember()
    {
        var createdMember = await CreateAsync<CreateMemberDto, ReadMemberDto>(
            new CreateMemberDto { FirstName = FirstNameJane, LastName = LastNameSmith });

        var getResponse = await GetByIdAsync(createdMember.Id);

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var fetchedMember = await getResponse.ReadPayloadAsync<ReadMemberDto>();

        Assert.That(fetchedMember, Is.Not.Null);
        Assert.That(fetchedMember, Is.EqualTo(createdMember));
    }

    [Test]
    public async Task GetById_ShouldReturnNotFound_WhenTheMemberDoesNotExist()
    {
        var getResponse = await GetByIdAsync(NonExistentMemberId);

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await getResponse.AssertThatProblemDetailsContain(
            NonExistentMemberId.ToString());
    }

    [Test]
    public async Task GetAll_ShouldReturnAllCreatedMembers_OrderedById()
    {
        var member1 = await TestDataFactory.CreateMemberAsync(FirstNameJane, LastNameSmith);
        var member2 = await TestDataFactory.CreateMemberAsync(FirstNameJohn, LastNameBrown);

        var response = await GetAllAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var members = await response.ReadPayloadAsync<List<ReadMemberDto>>();

        Assert.That(members, Is.Not.Null);
        Assert.That(members, Is.EqualTo([member1, member2]));

        response.AssertThatPaginationHeadersAre(totalCount: 2, totalPages: 1);
    }

    [Test]
    public async Task GetAll_ShouldReturnEmptyCollection_WhenNoMembersExist()
    {
        var response = await GetAllAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var members = await response.ReadPayloadAsync<List<ReadMemberDto>>();

        Assert.That(members, Is.Not.Null);
        Assert.That(members, Is.Empty);

        response.AssertThatPaginationHeadersAre(totalCount: 0, totalPages: 0);
    }

    [Test]
    public async Task GetAll_ShouldIncludeInactiveMembers()
    {
        var activeMember = await TestDataFactory.CreateMemberAsync(FirstNameJane, LastNameSmith);
        var inactiveMember = await TestDataFactory.CreateInactiveMemberAsync(FirstNameJohn, LastNameBrown);

        var response = await GetAllAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var members = await response.ReadPayloadAsync<List<ReadMemberDto>>();

        Assert.That(members, Is.Not.Null);
        Assert.That(
            members.Select(member => (member.Id, member.IsActive)),
            Is.EqualTo([(activeMember.Id, true), (inactiveMember.Id, false)]));
    }

    [Test]
    public async Task GetAll_ShouldRespectPagination_AndSetResponseHeaders()
    {
        for (int i = 1; i <= 5; ++i)
        {
            await TestDataFactory.CreateMemberAsync(lastName: $"Member{i}");
        }

        var response = await GetWithQuery(BaseUrl, new()
        {
            ["pageNumber"] = "2",
            ["pageSize"] = "3"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var members = await response.ReadPayloadAsync<List<ReadMemberDto>>();

        Assert.That(members, Is.Not.Null);
        Assert.That(
            members.Select(member => member.LastName),
            Is.EqualTo(["Member4", "Member5"]));

        response.AssertThatPaginationHeadersAre(totalCount: 5, totalPages: 2);
    }

    [Test]
    public async Task GetAll_ShouldFallBackToValidPagination_WhenParametersAreOutOfRange()
    {
        for (int i = 1; i <= 3; ++i)
        {
            await TestDataFactory.CreateMemberAsync(lastName: $"Member{i}");
        }

        var response = await GetWithQuery(BaseUrl, new()
        {
            ["pageNumber"] = "0",
            ["pageSize"] = "0"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var members = await response.ReadPayloadAsync<List<ReadMemberDto>>();

        Assert.That(members, Is.Not.Null);
        Assert.That(
            members.Select(member => member.LastName),
            Is.EqualTo(["Member1"]));

        response.AssertThatPaginationHeadersAre(totalCount: 3, totalPages: 3);
    }

    [Test]
    public async Task Create_ShouldReturnCreated_WithCorrectData_AndActiveStatus()
    {
        var createDto = new CreateMemberDto
        {
            FirstName = FirstNameJane,
            LastName = LastNameSmith
        };

        var createResponse = await PostAsync(createDto);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var createdMember = await createResponse.ReadPayloadAsync<ReadMemberDto>();

        Assert.That(createdMember, Is.Not.Null);
        Assert.That(createdMember.FirstName, Is.EqualTo(createDto.FirstName));
        Assert.That(createdMember.LastName, Is.EqualTo(createDto.LastName));
        Assert.That(createdMember.IsActive, Is.True);
        Assert.That(createdMember.Id, Is.GreaterThan(0));

        Assert.That(createResponse.GetLocation(),
            Is.EqualTo($"{BaseUrl}/{createdMember.Id}"));

        await AssertMemberExistsInTheDatabase(createdMember);
    }

    [Test]
    public async Task Create_ShouldAllowMembersWithTheSameName()
    {
        var firstMember = await TestDataFactory.CreateMemberAsync(FirstNameJane, LastNameSmith);

        var createResponse = await PostAsync(
            new CreateMemberDto { FirstName = FirstNameJane, LastName = LastNameSmith });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var secondMember = await createResponse.ReadPayloadAsync<ReadMemberDto>();

        Assert.That(secondMember, Is.Not.Null);
        Assert.That(secondMember.Id, Is.Not.EqualTo(firstMember.Id));
    }

    [Test]
    public async Task Create_ShouldReturnBadRequest_WhenNameIsInvalid()
    {
        var createDto = new CreateMemberDto
        {
            FirstName = string.Empty,
            LastName = LastNameSmith
        };

        var createResponse = await PostAsync(createDto);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await createResponse.AssertThatValidationErrorsContain("FirstName");
    }

    [Test]
    public async Task Update_ShouldReturnUpdatedMember()
    {
        var createdMember = await TestDataFactory.CreateMemberAsync(FirstNameJane, LastNameSmith);

        var updateDto = new UpdateMemberDto
        {
            FirstName = FirstNameJohn,
            LastName = LastNameBrown
        };
        var updateResponse = await PutAsync(updateDto, createdMember.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedMember = await updateResponse.ReadPayloadAsync<ReadMemberDto>();

        var expectedMember = createdMember with
        {
            FirstName = FirstNameJohn,
            LastName = LastNameBrown
        };

        Assert.That(updatedMember, Is.EqualTo(expectedMember));

        await AssertMemberExistsInTheDatabase(expectedMember);
    }

    [Test]
    public async Task Update_ShouldNotChangeActiveStatus()
    {
        var inactiveMember = await TestDataFactory.CreateInactiveMemberAsync(FirstNameJane, LastNameSmith);

        var updateDto = new UpdateMemberDto
        {
            FirstName = FirstNameJohn,
            LastName = LastNameBrown
        };
        var updateResponse = await PutAsync(updateDto, inactiveMember.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedMember = await updateResponse.ReadPayloadAsync<ReadMemberDto>();

        Assert.That(updatedMember, Is.Not.Null);
        Assert.That(updatedMember.IsActive, Is.False);
    }

    [Test]
    public async Task Update_ShouldReturnNotFound_WhenNoMemberWithTheGivenIdExists()
    {
        var updateDto = new UpdateMemberDto
        {
            FirstName = FirstNameJohn,
            LastName = LastNameBrown
        };
        var updateResponse = await PutAsync(updateDto, NonExistentMemberId);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await updateResponse.AssertThatProblemDetailsContain(
            NonExistentMemberId.ToString());
    }

    [Test]
    public async Task Update_ShouldReturnBadRequest_WhenNameIsInvalid()
    {
        var createdMember = await TestDataFactory.CreateMemberAsync(FirstNameJane, LastNameSmith);

        var updateDto = new UpdateMemberDto
        {
            FirstName = string.Empty,
            LastName = LastNameBrown
        };
        var updateResponse = await PutAsync(updateDto, createdMember.Id);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await updateResponse.AssertThatValidationErrorsContain("FirstName");

        await AssertMemberExistsInTheDatabase(createdMember);
    }

    [Test]
    public async Task Delete_ShouldReturnNoContent_AndDeactivateMember()
    {
        var createdMember = await TestDataFactory.CreateMemberAsync();

        var deleteResponse = await DeleteAsync(createdMember.Id);
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        await AssertMemberExistsInTheDatabase(createdMember with { IsActive = false });
    }

    [Test]
    public async Task Delete_ShouldReturnNoContent_WhenMemberIsAlreadyInactive()
    {
        var inactiveMember = await TestDataFactory.CreateInactiveMemberAsync();

        var deleteResponse = await DeleteAsync(inactiveMember.Id);
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        await AssertMemberExistsInTheDatabase(inactiveMember);
    }

    [Test]
    public async Task Delete_ShouldReturnNotFound_WhenNoMemberWithTheGivenIdExists()
    {
        var deleteResponse = await DeleteAsync(NonExistentMemberId);

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await deleteResponse.AssertThatProblemDetailsContain(
            NonExistentMemberId.ToString());
    }

    [Test]
    public async Task Delete_ShouldReturnConflict_WhenMemberHasActiveLoans()
    {
        var createdMember = await TestDataFactory.CreateMemberAsync();
        await TestDataFactory.CreateLoanForMemberAsync(createdMember.Id);

        var deleteResponse = await DeleteAsync(createdMember.Id);

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await deleteResponse.AssertThatProblemDetailsContain(
            createdMember.Id.ToString());

        await AssertMemberExistsInTheDatabase(createdMember);
    }

    [Test]
    public async Task Delete_ShouldReturnNoContent_WhenAllLoansOfTheMemberWereReturned()
    {
        var createdMember = await TestDataFactory.CreateMemberAsync();
        await TestDataFactory.CreateReturnedLoanForMemberAsync(createdMember.Id);

        var deleteResponse = await DeleteAsync(createdMember.Id);

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        await AssertMemberExistsInTheDatabase(createdMember with { IsActive = false });
    }

    [Test]
    public async Task Delete_ShouldNotBeBlocked_ByActiveLoansOfOtherMembers()
    {
        var memberToDelete = await TestDataFactory.CreateMemberAsync(FirstNameJane, LastNameSmith);
        var otherMember = await TestDataFactory.CreateMemberAsync(FirstNameJohn, LastNameBrown);
        await TestDataFactory.CreateLoanForMemberAsync(otherMember.Id);

        var deleteResponse = await DeleteAsync(memberToDelete.Id);

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Restore_ShouldReturnNoContent_AndActivateMember()
    {
        var inactiveMember = await TestDataFactory.CreateInactiveMemberAsync();

        var restoreResponse = await RestoreAsync(inactiveMember.Id);

        Assert.That(restoreResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        await AssertMemberExistsInTheDatabase(inactiveMember with { IsActive = true });
    }

    [Test]
    public async Task Restore_ShouldReturnNoContent_WhenMemberIsAlreadyActive()
    {
        var createdMember = await TestDataFactory.CreateMemberAsync();

        var restoreResponse = await RestoreAsync(createdMember.Id);

        Assert.That(restoreResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        await AssertMemberExistsInTheDatabase(createdMember);
    }

    [Test]
    public async Task Restore_ShouldReturnNotFound_WhenNoMemberWithTheGivenIdExists()
    {
        var restoreResponse = await RestoreAsync(NonExistentMemberId);

        Assert.That(restoreResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await restoreResponse.AssertThatProblemDetailsContain(
            NonExistentMemberId.ToString());
    }

    [Test]
    public async Task GetLoans_ShouldReturnNotFound_WhenMemberDoesNotExist()
    {
        var getLoansResponse = await Client.GetAsync(
            $"{BaseUrl}/{NonExistentMemberId}/loans");

        Assert.That(getLoansResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await getLoansResponse.AssertThatProblemDetailsContain(
            NonExistentMemberId.ToString());
    }

    [Test]
    public async Task GetLoans_ShouldReturnEmptyCollection_WhenMemberHasNoLoans()
    {
        var createdMember = await TestDataFactory.CreateMemberAsync();

        var getLoansResponse = await Client.GetAsync(
            $"{BaseUrl}/{createdMember.Id}/loans");

        Assert.That(getLoansResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var loans = await getLoansResponse.ReadPayloadAsync<List<ReadLoanDto>>();

        Assert.That(loans, Is.Not.Null);
        Assert.That(loans, Is.Empty);
    }

    [Test]
    public async Task GetLoans_ShouldReturnLoans_ForThatMemberOnly_OrderedByLoanDateDescending()
    {
        var member = await TestDataFactory.CreateMemberAsync(FirstNameJane, LastNameSmith);
        var otherMember = await TestDataFactory.CreateMemberAsync(FirstNameJohn, LastNameBrown);
        var book = await TestDataFactory.CreateBookAsync(totalCopies: 5);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var olderLoan = await TestDataFactory.CreateLoanAsync(
            book.Id, member.Id, loanDate: today.AddDays(-10));
        var newerLoan = await TestDataFactory.CreateLoanAsync(
            book.Id, member.Id, loanDate: today);
        await TestDataFactory.CreateLoanAsync(book.Id, otherMember.Id);

        var getLoansResponse = await Client.GetAsync(
            $"{BaseUrl}/{member.Id}/loans");

        Assert.That(getLoansResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var loans = await getLoansResponse.ReadPayloadAsync<List<ReadLoanDto>>();

        Assert.That(loans, Is.Not.Null);
        Assert.That(
            loans.Select(loan => loan.Id),
            Is.EqualTo([newerLoan.Id, olderLoan.Id]));
        Assert.That(
            loans.Select(loan => (loan.MemberFirstName, loan.MemberLastName, loan.BookTitle)),
            Has.All.EqualTo((FirstNameJane, LastNameSmith, book.Title)));
    }

    [Test]
    public async Task GetLoans_ShouldIncludeReturnedLoans()
    {
        var member = await TestDataFactory.CreateMemberAsync();
        await TestDataFactory.CreateReturnedLoanForMemberAsync(member.Id);

        var getLoansResponse = await Client.GetAsync(
            $"{BaseUrl}/{member.Id}/loans");

        Assert.That(getLoansResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var loans = await getLoansResponse.ReadPayloadAsync<List<ReadLoanDto>>();

        Assert.That(loans, Is.Not.Null);
        Assert.That(loans, Has.Count.EqualTo(1));
        Assert.That(loans[0].Status, Is.EqualTo(LoanStatus.Returned));
        Assert.That(loans[0].ReturnDate, Is.Not.Null);
    }

    private async Task<HttpResponseMessage> RestoreAsync(int id)
    {
        return await PatchWithoutBodyAsync($"{BaseUrl}/{id}/restore");
    }

    private async Task AssertMemberExistsInTheDatabase(ReadMemberDto expectedMember)
    {
        var getMemberResponse = await GetByIdAsync(expectedMember.Id);
        Assert.That(getMemberResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var memberFromGetResponse = await getMemberResponse.ReadPayloadAsync<ReadMemberDto>();
        Assert.That(memberFromGetResponse, Is.EqualTo(expectedMember));
    }
}
