using FluentValidation;

namespace BUnited.Modules.Chat.Application.UseCases.Client;

public sealed class ReportMessageValidator : AbstractValidator<ReportMessageRequest>
{
    public ReportMessageValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithErrorCode("errors.chat.reasonRequired");
    }
}
