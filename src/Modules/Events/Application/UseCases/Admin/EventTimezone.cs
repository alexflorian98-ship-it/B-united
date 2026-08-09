namespace BUnited.Modules.Events.Application.UseCases.Admin;

/// <summary>Validates a <c>DisplayTimezone</c> value against the OS/ICU timezone database
/// (IANA ids like "Europe/Bucharest") without hand-maintaining a list.</summary>
public static class EventTimezone
{
    public static bool IsValid(string timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            return false;
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
