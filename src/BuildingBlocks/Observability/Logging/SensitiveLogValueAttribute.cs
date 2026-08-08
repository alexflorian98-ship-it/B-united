namespace BUnited.BuildingBlocks.Observability.Logging;

/// <summary>
/// Marks a property as sensitive so <see cref="SensitiveDataDestructuringPolicy"/> redacts it
/// whenever the containing object is logged via structured destructuring (<c>{@Value}</c>).
/// Modules must apply this to any property that can hold a password, token, secret, card
/// payload, questionnaire answer or guidance text (see docs/DEVELOPMENT_INSTRUCTIONS.md §10).
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class SensitiveLogValueAttribute : Attribute
{
}
