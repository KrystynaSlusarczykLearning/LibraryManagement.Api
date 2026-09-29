using LibraryManagement.Api.RequestParameters;

namespace LibraryManagement.UnitTests.RequestParameters;

[TestFixture]
internal class PaginationParametersTests
{
    [Test]
    public void ShouldHaveDefaultValues_WhenNothingIsSet()
    {
        var cut = new PaginationParameters();

        Assert.That(cut.PageNumber, Is.EqualTo(PaginationParameters.DefaultPageNumber));
        Assert.That(cut.PageSize, Is.EqualTo(PaginationParameters.DefaultPageSize));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(1000)]
    [TestCase(int.MaxValue)]
    public void PageNumber_ShouldBeKept_WhenItIsPositive(int pageNumber)
    {
        var cut = new PaginationParameters { PageNumber = pageNumber };

        Assert.That(cut.PageNumber, Is.EqualTo(pageNumber));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void PageNumber_ShouldBeSetToOne_WhenItIsNotPositive(int pageNumber)
    {
        var cut = new PaginationParameters { PageNumber = pageNumber };

        Assert.That(cut.PageNumber, Is.EqualTo(1));
    }

    [TestCase(1)]
    [TestCase(10)]
    [TestCase(49)]
    [TestCase(50)]
    public void PageSize_ShouldBeKept_WhenItIsWithinAllowedRange(int pageSize)
    {
        var cut = new PaginationParameters { PageSize = pageSize };

        Assert.That(cut.PageSize, Is.EqualTo(pageSize));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void PageSize_ShouldBeSetToOne_WhenItIsNotPositive(int pageSize)
    {
        var cut = new PaginationParameters { PageSize = pageSize };

        Assert.That(cut.PageSize, Is.EqualTo(1));
    }

    [TestCase(51)]
    [TestCase(1000)]
    [TestCase(int.MaxValue)]
    public void PageSize_ShouldBeCappedAtFifty_WhenItIsTooLarge(int pageSize)
    {
        var cut = new PaginationParameters { PageSize = pageSize };

        Assert.That(cut.PageSize, Is.EqualTo(50));
    }

    [Test]
    public void PageNumberAndPageSize_ShouldBeNormalizedIndependently()
    {
        var cut = new PaginationParameters { PageNumber = -3, PageSize = 100 };

        Assert.That(cut.PageNumber, Is.EqualTo(1));
        Assert.That(cut.PageSize, Is.EqualTo(50));
    }
}
