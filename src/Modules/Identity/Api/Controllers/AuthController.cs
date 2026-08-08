using BUnited.BuildingBlocks.Security.Abuse;
using BUnited.Modules.Identity.Application.UseCases.Common;
using BUnited.Modules.Identity.Application.UseCases.Login;
using BUnited.Modules.Identity.Application.UseCases.PasswordReset;
using BUnited.Modules.Identity.Application.UseCases.Refresh;
using BUnited.Modules.Identity.Application.UseCases.Register;
using BUnited.Modules.Identity.Application.UseCases.Revoke;
using BUnited.Modules.Identity.Application.UseCases.VerifyEmail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BUnited.Modules.Identity.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    RegisterUserHandler registerUserHandler,
    VerifyEmailHandler verifyEmailHandler,
    ResendVerificationHandler resendVerificationHandler,
    LoginHandler loginHandler,
    RefreshTokenHandler refreshTokenHandler,
    RevokeTokenHandler revokeTokenHandler,
    RevokeAllSessionsHandler revokeAllSessionsHandler,
    RequestPasswordResetHandler requestPasswordResetHandler,
    ConfirmPasswordResetHandler confirmPasswordResetHandler) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicyName)]
    public async Task<ActionResult<RegisterUserResult>> Register(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await registerUserHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        await verifyEmailHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicyName)]
    public async Task<IActionResult> ResendVerification(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        await resendVerificationHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicyName)]
    public async Task<ActionResult<TokenPairResult>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await loginHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenPairResult>> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await refreshTokenHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RevokeTokenCommand command, CancellationToken cancellationToken)
    {
        await revokeTokenHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
    {
        await revokeAllSessionsHandler.HandleAsync(User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("password-reset/request")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicyName)]
    public async Task<IActionResult> RequestPasswordReset(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        await requestPasswordResetHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("password-reset/confirm")]
    public async Task<IActionResult> ConfirmPasswordReset(
        ConfirmPasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        await confirmPasswordResetHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }
}
