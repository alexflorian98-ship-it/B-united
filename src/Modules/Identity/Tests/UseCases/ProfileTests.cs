using BUnited.Modules.Identity.Application.UseCases.Profile;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace BUnited.Modules.Identity.Tests.UseCases;

public sealed class ProfileTests
{
    [Fact]
    public async Task Get_returns_the_users_email_and_preferences()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var user = User.Register("ada@example.com", "hash");
        context.Users.Add(user);
        context.UserPreferences.Add(UserPreference.CreateDefault(user.Id));
        await context.SaveChangesAsync();

        var handler = new GetProfileHandler(context);
        var result = await handler.HandleAsync(user.Id, CancellationToken.None);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("ada@example.com", result.Email);
        Assert.Equal("Europe/Bucharest", result.Timezone);
        Assert.Equal("ro", result.PreferredLanguage);
        Assert.True(result.EmailNotificationsEnabled);
    }

    [Fact]
    public async Task Update_persists_timezone_language_and_notification_preference()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var user = User.Register("ada@example.com", "hash");
        context.Users.Add(user);
        context.UserPreferences.Add(UserPreference.CreateDefault(user.Id));
        await context.SaveChangesAsync();

        var handler = new UpdateProfileHandler(context, NullLogger<UpdateProfileHandler>.Instance);
        var result = await handler.HandleAsync(
            new UpdateProfileCommand(user.Id, "America/New_York", "en", false),
            CancellationToken.None);

        Assert.Equal("America/New_York", result.Timezone);
        Assert.Equal("en", result.PreferredLanguage);
        Assert.False(result.EmailNotificationsEnabled);

        var reloaded = await new GetProfileHandler(context).HandleAsync(user.Id, CancellationToken.None);
        Assert.Equal("America/New_York", reloaded.Timezone);
        Assert.Equal("en", reloaded.PreferredLanguage);
        Assert.False(reloaded.EmailNotificationsEnabled);
    }

    [Fact]
    public async Task Update_does_not_change_a_different_users_profile()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var user = User.Register("ada@example.com", "hash");
        var otherUser = User.Register("bob@example.com", "hash");
        context.Users.AddRange(user, otherUser);
        context.UserPreferences.Add(UserPreference.CreateDefault(user.Id));
        context.UserPreferences.Add(UserPreference.CreateDefault(otherUser.Id));
        await context.SaveChangesAsync();

        var handler = new UpdateProfileHandler(context, NullLogger<UpdateProfileHandler>.Instance);
        await handler.HandleAsync(new UpdateProfileCommand(user.Id, "America/New_York", "en", false), CancellationToken.None);

        var otherProfile = await new GetProfileHandler(context).HandleAsync(otherUser.Id, CancellationToken.None);
        Assert.Equal("Europe/Bucharest", otherProfile.Timezone);
        Assert.Equal("ro", otherProfile.PreferredLanguage);
        Assert.True(otherProfile.EmailNotificationsEnabled);
    }
}
