using BUnited.Modules.Audit.Contracts;

namespace BUnited.Modules.Audit.Tests.Contracts;

public sealed class AuditEntryTests
{
    [Fact]
    public void Create_with_safe_metadata_succeeds()
    {
        var entry = AuditEntry.Create(
            AuditActions.UserLogin,
            actorUserId: Guid.NewGuid(),
            entityType: "User",
            entityId: Guid.NewGuid().ToString(),
            ipAddress: "203.0.113.10",
            metadata: new Dictionary<string, string> { ["loginMethod"] = "email" });

        Assert.Equal(AuditActions.UserLogin, entry.Action);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("Token")]
    [InlineData("refreshToken")]
    [InlineData("secretKey")]
    [InlineData("answerText")]
    [InlineData("guidanceNotes")]
    [InlineData("questionnaireResponse")]
    [InlineData("cardNumber")]
    [InlineData("cvv")]
    [InlineData("ssn")]
    [InlineData("apiKey")]
    [InlineData("Authorization")]
    [InlineData("credentials")]
    public void Create_rejects_metadata_keys_that_could_carry_sensitive_data(string forbiddenKey)
    {
        var metadata = new Dictionary<string, string> { [forbiddenKey] = "irrelevant-value" };

        Assert.Throws<ArgumentException>(
            () => AuditEntry.Create(AuditActions.UserLogin, metadata: metadata));
    }

    [Fact]
    public void Create_without_action_throws()
    {
        Assert.Throws<ArgumentException>(() => AuditEntry.Create(string.Empty));
    }

    [Fact]
    public void Create_allows_null_metadata()
    {
        var entry = AuditEntry.Create(AuditActions.UserFailedLogin);

        Assert.Null(entry.Metadata);
    }
}
