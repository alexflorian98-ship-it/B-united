namespace BUnited.Modules.Identity.Application.UseCases.Admin.Users;

public sealed record RemoveClientRoleCommand(Guid ActorUserId, Guid TargetUserId, Guid RoleId);
