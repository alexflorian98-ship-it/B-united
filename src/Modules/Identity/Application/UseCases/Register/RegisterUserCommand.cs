using BUnited.BuildingBlocks.Observability.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.Register;

public sealed record RegisterUserCommand(string Email, [property: SensitiveLogValue] string Password);

public sealed record RegisterUserResult(Guid UserId, string Email);
