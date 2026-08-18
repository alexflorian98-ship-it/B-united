using BUnited.BuildingBlocks.Observability.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.Login;

public sealed record LoginCommand(string Email, [property: SensitiveLogValue] string Password);
