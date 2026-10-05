using ErrorOr;

namespace LibraryManagement.Api.Errors;

public static class AuthenticationErrors
{
    public static Error RegistrationFailed(string description) =>
        Error.Validation(description: description);
}
