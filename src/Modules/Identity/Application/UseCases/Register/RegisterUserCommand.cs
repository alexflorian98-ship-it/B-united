namespace BUnited.Modules.Identity.Application.UseCases.Register;

public sealed record RegisterUserCommand(string Email, string Password);

public sealed record RegisterUserResult(Guid UserId, string Email);
