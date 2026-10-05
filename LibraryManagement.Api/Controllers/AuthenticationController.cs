using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.DTOs.Users;
using LibraryManagement.Api.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/authentication")]
public class AuthenticationController(
    IAuthenticationService authenticationService) : WebApiController
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ReadMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReadMemberDto>> Register(
        RegisterMemberUserDto registerDto)
    {
        var result = await authenticationService.RegisterMemberAsync(registerDto);

        if(result.IsError)
        {
            return Problem(result.FirstError);
        }

        return CreatedAtRoute(
            "GetMemberById",
            new { id = result.Value.Id },
            result.Value);
    }
}
