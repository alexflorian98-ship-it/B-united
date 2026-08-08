namespace BUnited.Modules.Identity.Application.UseCases.PasswordReset;

public sealed record ConfirmPasswordResetCommand(string Token, string NewPassword);
