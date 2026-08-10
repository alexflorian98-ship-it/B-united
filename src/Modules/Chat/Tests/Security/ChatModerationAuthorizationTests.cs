using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Identity.Contracts;

namespace BUnited.Modules.Chat.Tests.Security;

/// <summary>P6.20.a — HTTP-level proof that <c>chat.moderate</c> gates the real
/// <c>AdminChatController</c> moderation endpoints: an authorized moderator can act, a regular
/// authenticated user (and an anonymous caller) cannot. Only live-verified via curl before this
/// test existed.</summary>
public sealed class ChatModerationAuthorizationTests : IClassFixture<ChatAdminApiTestHostFixture>
{
    private readonly ChatAdminApiTestHostFixture _fixture;

    public ChatModerationAuthorizationTests(ChatAdminApiTestHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Moderator_with_chat_moderate_permission_can_mute_a_user()
    {
        var token = _fixture.IssueToken([WellKnownPermissionKeys.ChatModerate]);
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var targetUserId = Guid.NewGuid();

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/v1/admin/chat/users/{targetUserId}/mute",
            new { DurationMinutes = 60, Reason = "Spam" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var assertionContext = _fixture.OpenAssertionContext();
        Assert.Single(assertionContext.Set<Mute>().Where(m => m.UserId == targetUserId));
    }

    [Fact]
    public async Task Authenticated_user_without_chat_moderate_permission_is_forbidden_from_muting()
    {
        var token = _fixture.IssueToken([]);
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var targetUserId = Guid.NewGuid();

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/v1/admin/chat/users/{targetUserId}/mute",
            new { DurationMinutes = 60, Reason = "Spam" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var assertionContext = _fixture.OpenAssertionContext();
        Assert.Empty(assertionContext.Set<Mute>().Where(m => m.UserId == targetUserId));
    }

    [Fact]
    public async Task Anonymous_caller_is_unauthorized_from_muting()
    {
        _fixture.Client.DefaultRequestHeaders.Authorization = null;
        var targetUserId = Guid.NewGuid();

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/v1/admin/chat/users/{targetUserId}/mute",
            new { DurationMinutes = 60, Reason = "Spam" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
