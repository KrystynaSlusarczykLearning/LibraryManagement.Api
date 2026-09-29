using LibraryManagement.Api.RequestParameters;
using LibraryManagement.Api.RequestParameters.Enums;

namespace LibraryManagement.UnitTests.RequestParameters;

[TestFixture]
internal class SortingParametersTests
{
    [Test]
    public void ShouldHaveNoSortByAndAscendingDirection_WhenNothingIsSet()
    {
        var cut = new SortingParameters<LoanSortBy>();

        Assert.That(cut.SortBy, Is.Null);
        Assert.That(cut.SortDirection, Is.EqualTo(SortDirection.Asc));
    }
}
