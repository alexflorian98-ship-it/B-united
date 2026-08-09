using FluentValidation;

namespace BUnited.Modules.Events.Application.UseCases.Admin;

public sealed class UpdateEventScheduleValidator : AbstractValidator<UpdateEventScheduleRequest>
{
    public UpdateEventScheduleValidator()
    {
        RuleFor(x => x.DisplayTimezone).NotEmpty().Must(EventTimezone.IsValid).WithErrorCode("errors.event.timezoneInvalid");
        RuleFor(x => x.EndsAtUtc).GreaterThan(x => x.StartsAtUtc).WithErrorCode("errors.event.endBeforeStart");
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue).WithErrorCode("errors.event.capacityInvalid");
        RuleFor(x => x.MeetingUrl).NotEmpty().When(x => x.LocationType == Domain.EventLocationType.Online).WithErrorCode("errors.event.meetingUrlRequired");
        RuleFor(x => x.Location).NotEmpty().When(x => x.LocationType == Domain.EventLocationType.Physical).WithErrorCode("errors.event.locationRequired");
    }
}
