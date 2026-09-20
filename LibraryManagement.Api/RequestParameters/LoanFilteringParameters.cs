using LibraryManagement.Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.RequestParameters;

public record LoanFilteringParameters(
     [property: FromQuery(Name = "loanStatus")] LoanStatus? LoanStatus,
     [property: FromQuery(Name = "bookId")] int? BookId,
     [property: FromQuery(Name = "memberId")] int? MemberId);
