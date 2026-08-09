namespace BUnited.Modules.Identity.Application.UseCases.Admin.Users;

public sealed record RoleSummaryDto(Guid Id, string Name);

public sealed record ClientListItemDto(
    Guid Id,
    string Email,
    bool IsActive,
    bool IsEmailVerified,
    DateTime CreatedAt,
    IReadOnlyList<RoleSummaryDto> Roles);

public sealed record ClientListResult(IReadOnlyList<ClientListItemDto> Items, int TotalCount, int Page, int PageSize);

public sealed record ClientDetailDto(
    Guid Id,
    string Email,
    bool IsActive,
    bool IsEmailVerified,
    DateTime? EmailVerifiedAtUtc,
    DateTime CreatedAt,
    IReadOnlyList<RoleSummaryDto> Roles);
