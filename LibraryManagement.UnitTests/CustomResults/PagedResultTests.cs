using LibraryManagement.Api.CustomResults;
using LibraryManagement.Api.RequestParameters;

namespace LibraryManagement.UnitTests.CustomResults;

[TestFixture]
internal class PagedResultTests
{
    [TestCase(0, 10, 0)]
    [TestCase(1, 10, 1)]
    [TestCase(9, 10, 1)]
    [TestCase(10, 10, 1)]
    [TestCase(11, 10, 2)]
    [TestCase(5, 3, 2)]
    [TestCase(6, 3, 2)]
    [TestCase(7, 1, 7)]
    [TestCase(100, 50, 2)]
    public void TotalPages_ShouldBeRoundedUp(int totalCount, int pageSize, int expectedTotalPages)
    {
        var cut = new PagedResult<int>(
            [],
            totalCount,
            new PaginationParameters { PageSize = pageSize });

        Assert.That(cut.TotalPages, Is.EqualTo(expectedTotalPages));
    }

    [Test]
    public void TotalPages_ShouldUseNormalizedPageSize()
    {
        var cut = new PagedResult<int>(
            [],
            120,
            new PaginationParameters { PageSize = 1000 });

        Assert.That(cut.TotalPages, Is.EqualTo(3));
    }

    [Test]
    public void TotalPages_ShouldNotDependOnNumberOfItems()
    {
        var cut = new PagedResult<int>(
            [1, 2],
            25,
            new PaginationParameters { PageNumber = 3, PageSize = 10 });

        Assert.That(cut.TotalPages, Is.EqualTo(3));
    }
}
