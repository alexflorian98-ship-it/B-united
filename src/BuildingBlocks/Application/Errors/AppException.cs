namespace BUnited.BuildingBlocks.Application.Errors;

/// <summary>
/// Base type for expected, anticipated application/business errors that should reach the
/// client as a structured response (docs/ARCHITECTURE.md §24) instead of an opaque 500.
/// Deliberately framework-agnostic (no HTTP concepts) — the Api host maps exception type to
/// HTTP status code. <see cref="Code"/> and <see cref="MessageKey"/> must be fixed, stable
/// values owned by the concrete exception type; never build them from ad hoc call-site strings
/// (docs/DEVELOPMENT_INSTRUCTIONS.md §3).
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string code, string messageKey, string message)
        : base(message)
    {
        Code = code;
        MessageKey = messageKey;
    }

    public string Code { get; }

    public string MessageKey { get; }
}

/// <summary>
/// Thrown when a requested resource does not exist or is not visible to the current caller.
/// Maps to HTTP 404.
/// </summary>
public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string message)
        : base("NOT_FOUND", "errors.notFound", message)
    {
    }
}

/// <summary>
/// Thrown for a named business-rule violation that isn't a validation or not-found case (e.g.
/// docs/ARCHITECTURE.md §24's <c>SUBSCRIPTION_INACTIVE</c> example). Maps to HTTP 400. Each
/// call site should reference a shared constant for <paramref name="code"/>/
/// <paramref name="messageKey"/> owned by the throwing module rather than inlining literals
/// at multiple call sites.
/// </summary>
public sealed class BusinessRuleAppException : AppException
{
    public BusinessRuleAppException(string code, string messageKey, string message)
        : base(code, messageKey, message)
    {
    }
}
