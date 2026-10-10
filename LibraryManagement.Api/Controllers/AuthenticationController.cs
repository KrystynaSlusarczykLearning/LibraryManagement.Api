using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.DTOs.Users;
using LibraryManagement.Api.Entities;
using LibraryManagement.Api.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize(Roles = Roles.Librarian)]
    [HttpPost("register-librarian")]
    [ProducesResponseType(typeof(ReadMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> RegisterLibrarian(
      RegisterLibrarianUserDto registerDto)
    {
        var result = await authenticationService.RegisterLibrarianAsync(registerDto);

        if (result.IsError)
        {
            return Problem(result.FirstError);
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<string>> Login(LoginDto loginDto)
    {
        var result = await authenticationService.LoginAsync(loginDto);

        if (result.IsError)
        {
            return Problem(result.FirstError);
        }

        return Ok(result.Value);
    }
}
