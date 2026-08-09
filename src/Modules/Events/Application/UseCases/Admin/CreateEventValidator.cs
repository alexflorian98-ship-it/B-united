using FluentValidation;

namespace BUnited.Modules.Events.Application.UseCases.Admin;

public sealed class CreateEventValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.DefaultLanguage).NotEmpty().WithErrorCode("errors.event.languageRequired");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithErrorCode("errors.event.titleRequired");
        RuleFor(x => x.Description).NotEmpty().WithErrorCode("errors.event.descriptionRequired");
        RuleFor(x => x.DisplayTimezone).NotEmpty().Must(EventTimezone.IsValid).WithErrorCode("errors.event.timezoneInvalid");
        RuleFor(x => x.EndsAtUtc).GreaterThan(x => x.StartsAtUtc).WithErrorCode("errors.event.endBeforeStart");
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue).WithErrorCode("errors.event.capacityInvalid");
        RuleFor(x => x.MeetingUrl).NotEmpty().When(x => x.LocationType == Domain.EventLocationType.Online).WithErrorCode("errors.event.meetingUrlRequired");
        RuleFor(x => x.Location).NotEmpty().When(x => x.LocationType == Domain.EventLocationType.Physical).WithErrorCode("errors.event.locationRequired");
    }
}
