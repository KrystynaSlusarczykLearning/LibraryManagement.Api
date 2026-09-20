using ErrorOr;

namespace LibraryManagement.Api.Errors;

public static class BookErrors
{
    public static Error CategoryNotFound(int id) =>
        Error.Validation(
                description: $"Category with Id {id} does not exist.");

    public static Error AuthorsNotFound() =>
        Error.Validation(
                description: "One or more of the provided author ids is not valid.");

    public static Error BookNotFound(int id) =>
        Error.NotFound(
                description: $"A book with Id {id} has not been found.");

    public static Error HasLoans(int id) =>
        Error.Conflict(
                description: $"A book with Id {id} cannot be deleted, because " +
                             $"it has loans associated with it.");

    public static Error NotEnoughAvailableCopies(int id) =>
        Error.Conflict(
                description: $"A book with Id {id} cannot have TotalCopies " +
                             $"reduced below the number of currently active loans.");
}
