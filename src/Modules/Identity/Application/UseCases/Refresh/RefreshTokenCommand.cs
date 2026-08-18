using BUnited.BuildingBlocks.Observability.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.Refresh;

public sealed record RefreshTokenCommand([property: SensitiveLogValue] string RefreshToken);
