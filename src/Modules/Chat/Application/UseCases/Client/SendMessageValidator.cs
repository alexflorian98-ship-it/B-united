using FluentValidation;

namespace BUnited.Modules.Chat.Application.UseCases.Client;

public sealed class SendMessageValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000).WithErrorCode("errors.chat.bodyRequired");
    }
}
