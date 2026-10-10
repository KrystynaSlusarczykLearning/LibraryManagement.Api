using ErrorOr;

namespace LibraryManagement.Api.Errors;

public static class AuthenticationErrors
{
    public static Error RegistrationFailed(string description) =>
        Error.Validation(description: description);

    public static Error InvalidCredentials() =>
        Error.Unauthorized(description: "The email or password is incorrect.");
}
