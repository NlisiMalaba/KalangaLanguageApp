using Kalanga.Api.Contracts.Auth;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserResult>> Register(
        [FromBody] RegisterRequest request,
        [FromServices] RegisterUserPort registerUser,
        CancellationToken cancellationToken)
    {
        var result = await registerUser.ExecuteAsync(
            new RegisterUserCommand(
                LanguageId.From(request.LanguageId),
                request.Email,
                request.Password,
                request.DisplayName),
            cancellationToken);

        return Created($"/auth/register/{result.UserId.Value}", result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticateUserResult>> Login(
        [FromBody] LoginRequest request,
        [FromServices] AuthenticateUserPort authenticateUser,
        CancellationToken cancellationToken)
    {
        var result = await authenticateUser.ExecuteAsync(
            new AuthenticateUserCommand(
                LanguageId.From(request.LanguageId),
                request.Email,
                request.Password),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthenticateUserResult>> Refresh(
        [FromBody] RefreshRequest request,
        [FromServices] RefreshTokenPort refreshToken,
        CancellationToken cancellationToken)
    {
        var result = await refreshToken.ExecuteAsync(
            new RefreshTokenCommand(LanguageId.From(request.LanguageId), request.RefreshToken),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        [FromServices] LogoutPort logout,
        CancellationToken cancellationToken)
    {
        await logout.ExecuteAsync(
            new LogoutCommand(LanguageId.From(request.LanguageId), request.RefreshToken),
            cancellationToken);

        return NoContent();
    }
}
