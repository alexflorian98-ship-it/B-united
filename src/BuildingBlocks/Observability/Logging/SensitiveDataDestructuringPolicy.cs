using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace BUnited.BuildingBlocks.Observability.Logging;

/// <summary>
/// Serilog destructuring policy that replaces the value of any property marked with
/// <see cref="SensitiveLogValueAttribute"/> with a fixed redaction marker before the object
/// is written to a log event, so a structured log call (<c>{@Value}</c>) can never leak the
/// raw value even if a caller forgets to log a projection instead of the entity/DTO itself.
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private const string RedactedValue = "***REDACTED***";

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> SensitivePropertiesCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> AllPropertiesCache = new();

    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        [NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        result = null;
        var type = value.GetType();

        var sensitiveProperties = SensitivePropertiesCache.GetOrAdd(type, GetSensitiveProperties);
        if (sensitiveProperties.Length == 0)
        {
            return false;
        }

        var allProperties = AllPropertiesCache.GetOrAdd(type, GetReadableProperties);
        var logProperties = new List<LogEventProperty>(allProperties.Length);

        foreach (var property in allProperties)
        {
            var isSensitive = Array.IndexOf(sensitiveProperties, property) >= 0;
            var rawValue = isSensitive ? RedactedValue : SafeGetValue(property, value);
            logProperties.Add(new LogEventProperty(property.Name, propertyValueFactory.CreatePropertyValue(rawValue, destructureObjects: true)));
        }

        result = new StructureValue(logProperties, type.Name);
        return true;
    }

    private static PropertyInfo[] GetSensitiveProperties(Type type) =>
        GetReadableProperties(type)
            .Where(p => p.GetCustomAttribute<SensitiveLogValueAttribute>() is not null)
            .ToArray();

    private static PropertyInfo[] GetReadableProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();

    private static object? SafeGetValue(PropertyInfo property, object instance)
    {
        try
        {
            return property.GetValue(instance);
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }
}
