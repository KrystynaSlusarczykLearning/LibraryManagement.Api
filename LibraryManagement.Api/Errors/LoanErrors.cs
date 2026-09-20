using ErrorOr;

namespace LibraryManagement.Api.Errors;

public static class LoanErrors
{
    public static Error LoanNotFound(int id) =>
        Error.NotFound(
                description: $"A loan with Id {id} has not been found.");

    public static Error AlreadyReturned(int id) =>
        Error.Conflict(
                description: $"A loan with Id {id} has already been returned.");

    public static Error MemberNotFound(int memberId) =>
        Error.Validation(
                description: $"A member with Id {memberId} has not been found.");

    public static Error MemberInactive(int memberId) =>
        Error.Conflict(
                description: $"A member with Id {memberId} is inactive.");

    public static Error BookNotFound(int bookId) =>
        Error.Validation(
                description: $"A book with Id {bookId} has not been found.");

    public static Error NoAvailableCopies(int bookId) =>
        Error.Conflict(
                description: $"A book with Id {bookId} has no available copies.");

    public static Error LoanDateAfterDueDate() =>
        Error.Validation(
                description: $"Due date must be after loan date.");
}