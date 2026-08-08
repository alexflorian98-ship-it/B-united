using BUnited.Modules.Identity.Application.Abstractions;
using BUnited.Modules.Identity.Domain;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.Register;

public sealed class RegisterUserHandler(
    DbContext dbContext,
    IPasswordHasher passwordHasher,
    ISecureTokenGenerator tokenGenerator,
    IIdentityEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<RegisterUserHandler> logger)
{
    public async Task<RegisterUserResult> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = User.Register(command.Email, passwordHash);
        user.AssignRole(WellKnownRoles.ClientId);

        dbContext.Set<User>().Add(user);
        dbContext.Set<UserPreference>().Add(UserPreference.CreateDefault(user.Id));

        var (rawToken, tokenHash) = tokenGenerator.Generate();
        var verificationToken = EmailVerificationToken.Issue(user.Id, tokenHash, utcNow, utcNow.AddHours(24));
        dbContext.Set<EmailVerificationToken>().Add(verificationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await emailSender.SendEmailVerificationAsync(user.Email, rawToken, cancellationToken);

        logger.LogInformation("identity.user_registered: UserId {UserId}", user.Id);

        return new RegisterUserResult(user.Id, user.Email);
    }
}
