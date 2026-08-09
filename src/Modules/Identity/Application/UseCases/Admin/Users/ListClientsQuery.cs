namespace BUnited.Modules.Identity.Application.UseCases.Admin.Users;

/// <summary><paramref name="Search"/> matches (case-insensitive, substring) against email.
/// <paramref name="RoleId"/> filters to users holding that exact role, when provided.</summary>
public sealed record ListClientsQuery(string? Search, Guid? RoleId, int Page, int PageSize);
