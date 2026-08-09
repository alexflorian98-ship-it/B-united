using FluentValidation;

namespace BUnited.Modules.Chat.Application.UseCases.Admin;

public sealed class CreateChatRoomValidator : AbstractValidator<CreateChatRoomRequest>
{
    public CreateChatRoomValidator()
    {
        RuleFor(x => x.ProgramId).NotEmpty().WithErrorCode("errors.chat.roomProgramRequired");
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100).WithErrorCode("errors.chat.roomKeyRequired");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithErrorCode("errors.chat.roomNameRequired");
    }
}

public sealed class UpdateChatRoomValidator : AbstractValidator<UpdateChatRoomRequest>
{
    public UpdateChatRoomValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithErrorCode("errors.chat.roomNameRequired");
    }
}
