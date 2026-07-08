using System.Security.Authentication;
using Eventum.Users.Application.DTO;
using Eventum.Users.Application.Exceptions;
using Eventum.Users.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Eventum.Users.WebApi.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController(RegisterUser registerUser, LoginUser loginUser) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken token)
    {
        try
        {
            await registerUser.Execute(request, token);
            return NoContent();
        }
        catch (UserAlreadyExistsException ex)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Detail = ex.Message });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken token)
    {
        try
        {
            var response = await loginUser.Execute(request, token);
            return Ok(response);
        }
        catch (InvalidCredentialException ex)
        {
            return Unauthorized(new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Detail = ex.Message });
        }
    }
}
