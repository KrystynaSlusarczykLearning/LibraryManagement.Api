using Microsoft.AspNetCore.Http.HttpResults;

namespace LibraryManagement.Api.Errors;

public static class MemberErrors
{
    public static ProblemHttpResult NotFound(int id) =>
        TypedResults.Problem(
            detail: $"A member with Id {id} has not been found.",
            statusCode: StatusCodes.Status404NotFound);

    public static ProblemHttpResult HasActiveLoans(int id) =>
        TypedResults.Problem(
                detail: $"A member with Id {id} cannot be deactivated because " +
                        $"they have active loans.",
                statusCode: StatusCodes.Status409Conflict);

    public static ProblemHttpResult Forbidden() =>
        TypedResults.Problem(
                detail: "The current user is not allowed to access this resource.",
                statusCode: StatusCodes.Status403Forbidden);
}
