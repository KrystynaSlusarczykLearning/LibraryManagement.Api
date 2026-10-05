using ErrorOr;
using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.DTOs.Users;
using LibraryManagement.Api.Entities;
using LibraryManagement.Api.Errors;
using LibraryManagement.Api.Mapping;
using LibraryManagement.Api.Services.Contracts;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Api.Services;

public class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    LibraryDbContext dbContext,
    MemberMapper mapper) : IAuthenticationService
{
    public async Task<ErrorOr<ReadMemberDto>> RegisterMemberAsync(
        RegisterMemberUserDto registerDto)
    {
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            Email = registerDto.Email,
            UserName = registerDto.Email
        };

        var result = await userManager.CreateAsync(user, registerDto.Password);

        if(!result.Succeeded)
        {
            return AuthenticationErrors.RegistrationFailed(
                result.Errors.First().Description);
        }

        await userManager.AddToRoleAsync(user, Roles.Member);

        var member = new Member
        {
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            IsActive = true,
            UserId = user.Id
        };

        dbContext.Members.Add(member);

        await dbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return mapper.ToReadDto(member);
    }
}
