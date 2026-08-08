using BUnited.Modules.Identity.Application.UseCases.Register;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Tests.TestSupport;

namespace BUnited.Modules.Identity.Tests.UseCases;

public sealed class RegisterUserValidatorTests
{
    [Fact]
    public async Task Valid_command_passes()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var validator = new RegisterUserValidator(context);

        var result = await validator.ValidateAsync(new RegisterUserCommand("new@example.com", "StrongPass123"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Rejects_an_already_registered_email()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        context.Users.Add(User.Register("taken@example.com", "hash"));
        await context.SaveChangesAsync();

        var validator = new RegisterUserValidator(context);

        var result = await validator.ValidateAsync(new RegisterUserCommand("taken@example.com", "StrongPass123"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.Email)
            && e.ErrorCode == "errors.email.alreadyRegistered");
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public async Task Rejects_weak_passwords(string weakPassword)
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var validator = new RegisterUserValidator(context);

        var result = await validator.ValidateAsync(new RegisterUserCommand("new@example.com", weakPassword));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.Password));
    }
}
