using BUnited.Modules.Identity.Contracts;

namespace BUnited.Modules.Questionnaires.Tests.TestSupport;

internal sealed class FakeConsentContext : IConsentContext
{
    private readonly HashSet<(Guid UserId, string ConsentType, int Version)> _consents = [];

    public Task<bool> HasConsentedAsync(Guid userId, string consentType, int requiredVersion, CancellationToken cancellationToken) =>
        Task.FromResult(_consents.Any(c => c.UserId == userId && c.ConsentType == consentType && c.Version >= requiredVersion));

    public Task RecordConsentAsync(Guid userId, string consentType, int version, CancellationToken cancellationToken)
    {
        _consents.Add((userId, consentType, version));
        return Task.CompletedTask;
    }
}
