using ErrorOr;
using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.DTOs.Users;

namespace LibraryManagement.Api.Services.Contracts;

public interface IAuthenticationService
{
    Task<ErrorOr<ReadMemberDto>> RegisterMemberAsync(
        RegisterMemberUserDto registerDto);
}
