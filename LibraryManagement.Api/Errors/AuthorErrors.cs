using ErrorOr;
using LibraryManagement.Api.DTOs.Members;

namespace LibraryManagement.Api.Errors;

public static class AuthorErrors
{
    public static Error AuthorNotFound(int id) =>
        Error.NotFound(
                description: $"An author with Id {id} has not been found.");

    public static Error AuthorHasBooks(int id) =>
        Error.Conflict(
                description: $"Cannot delete the author with Id {id}, " +
                             $"because they have books assigned. " +
                             $"Please remove or reassign the books first.");
}
