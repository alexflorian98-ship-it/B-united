using FluentValidation;

namespace BUnited.Modules.Chat.Application.UseCases.Moderation;

public sealed class MuteUserValidator : AbstractValidator<MuteUserRequest>
{
    public MuteUserValidator()
    {
        RuleFor(x => x.DurationMinutes).GreaterThan(0).WithErrorCode("errors.chat.muteDurationInvalid");
        RuleFor(x => x.Reason).MaximumLength(500).WithErrorCode("errors.chat.reasonTooLong");
    }
}
