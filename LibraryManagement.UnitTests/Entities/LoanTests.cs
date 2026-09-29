using LibraryManagement.Api.Entities;

namespace LibraryManagement.UnitTests.Entities;

[TestFixture]
internal class LoanTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    [Test]
    public void Status_ShouldBeActive_WhenNotReturnedAndDueDateIsInTheFuture()
    {
        var cut = CreateLoan(dueDate: Today.AddDays(1));

        Assert.That(cut.Status, Is.EqualTo(LoanStatus.Active));
    }

    [Test]
    public void Status_ShouldBeActive_WhenNotReturnedAndDueDateIsToday()
    {
        var cut = CreateLoan(dueDate: Today);

        Assert.That(cut.Status, Is.EqualTo(LoanStatus.Active));
    }

    [Test]
    public void Status_ShouldBeOverdue_WhenNotReturnedAndDueDateHasPassed()
    {
        var cut = CreateLoan(dueDate: Today.AddDays(-1));

        Assert.That(cut.Status, Is.EqualTo(LoanStatus.Overdue));
    }

    [Test]
    public void Status_ShouldBeReturned_WhenReturnedBeforeDueDate()
    {
        var cut = CreateLoan(dueDate: Today.AddDays(7), returnDate: Today);

        Assert.That(cut.Status, Is.EqualTo(LoanStatus.Returned));
    }

    [Test]
    public void Status_ShouldBeReturned_WhenReturnedAndDueDateHasPassed()
    {
        var cut = CreateLoan(dueDate: Today.AddDays(-7), returnDate: Today);

        Assert.That(cut.Status, Is.EqualTo(LoanStatus.Returned));
    }

    private static Loan CreateLoan(DateOnly dueDate, DateOnly? returnDate = null)
    {
        return new Loan
        {
            BookId = 1,
            MemberId = 1,
            LoanDate = dueDate.AddDays(-14),
            DueDate = dueDate,
            ReturnDate = returnDate
        };
    }
}
