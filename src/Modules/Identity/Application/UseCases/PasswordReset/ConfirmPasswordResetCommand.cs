using BUnited.BuildingBlocks.Observability.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.PasswordReset;

public sealed record ConfirmPasswordResetCommand(
    [property: SensitiveLogValue] string Token,
    [property: SensitiveLogValue] string NewPassword);
