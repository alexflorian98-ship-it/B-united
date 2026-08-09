namespace BUnited.Modules.Identity.Application.UseCases.Admin.Users;

public sealed record AssignClientRoleCommand(Guid ActorUserId, Guid TargetUserId, Guid RoleId);
