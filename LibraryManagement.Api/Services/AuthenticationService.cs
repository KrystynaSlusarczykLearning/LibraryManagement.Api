using ErrorOr;
using LibraryManagement.Api.Common;
using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.DTOs.Users;
using LibraryManagement.Api.Entities;
using LibraryManagement.Api.Errors;
using LibraryManagement.Api.Mapping;
using LibraryManagement.Api.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LibraryManagement.Api.Services;

public class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    LibraryDbContext dbContext,
    MemberMapper mapper,
    IOptions<JwtSettings> jwtSettings) : IAuthenticationService
{
    public async Task<ErrorOr<ReadMemberDto>> RegisterMemberAsync(
        RegisterMemberUserDto registerDto)
    {
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        var userRegistrationResult = await RegisterUserAsync(
            registerDto.Email, registerDto.Password, Roles.Member);

        if (userRegistrationResult.IsError)
        {
            return userRegistrationResult.FirstError;
        }

        var user = userRegistrationResult.Value;

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

    public async Task<ErrorOr<Created>> RegisterLibrarianAsync(
        RegisterLibrarianUserDto registerDto)
    {
        var userRegistrationResult = await RegisterUserAsync(
            registerDto.Email, registerDto.Password, Roles.Librarian);

        if(userRegistrationResult.IsError)
        {
            return userRegistrationResult.FirstError;
        }

        return Result.Created;
    }

    public async Task<ErrorOr<string>> LoginAsync(LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);

        if (user is null)
        {
            return AuthenticationErrors.InvalidCredentials();
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(
            user, loginDto.Password);

        if (!isPasswordValid)
        {
            return AuthenticationErrors.InvalidCredentials();
        }

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.First();

        return GenerateToken(user, role);
    }

    private async Task<ErrorOr<ApplicationUser>> RegisterUserAsync(
        string email, string password, string role)
    {
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return AuthenticationErrors.RegistrationFailed(
                result.Errors.First().Description);
        }

        await userManager.AddToRoleAsync(user, role);

        return user;
    }

    private string GenerateToken(ApplicationUser user, string role)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.Role, role),
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Value.Key));
        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Value.Issuer,
            audience: jwtSettings.Value.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.Value.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
 