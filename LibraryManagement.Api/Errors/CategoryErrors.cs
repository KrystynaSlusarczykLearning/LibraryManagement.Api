using Microsoft.AspNetCore.Http.HttpResults;

namespace LibraryManagement.Api.Errors;

public static class CategoryErrors
{
    public static ProblemHttpResult NotFound(int id) =>
        TypedResults.Problem(
                detail: $"A category with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);

    public static ProblemHttpResult HasActiveLoans(int id) =>
        TypedResults.Problem(
                detail: $"A member with Id {id} cannot be deactivated because " +
                        $"they have active loans.",
                statusCode: StatusCodes.Status409Conflict);

    public static ProblemHttpResult NameAlreadyTaken(string name) =>
        TypedResults.Problem(
            detail: $"A category with Name {name} already exists.",
            statusCode: StatusCodes.Status409Conflict);

    public static ProblemHttpResult HasBooksAssigned(int id) =>
        TypedResults.Problem(
                detail: $"Cannot delete the category with Id {id}, " +
                        $"because it has books assigned. " +
                        $"Please remove or reassign the books first.",
                statusCode: StatusCodes.Status409Conflict);
}