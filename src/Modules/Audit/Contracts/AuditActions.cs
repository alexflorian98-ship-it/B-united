namespace BUnited.Modules.Audit.Contracts;

/// <summary>
/// Canonical audit action names (docs/PROMPT.md §37). The single owned definition of these
/// strings — callers must reference these constants rather than inlining action-name literals
/// (docs/DEVELOPMENT_INSTRUCTIONS.md §3).
/// </summary>
public static class AuditActions
{
    public const string UserLogin = "user.login";
    public const string UserFailedLogin = "user.failed_login";
    public const string UserPasswordReset = "user.password_reset";
    public const string UserRoleChanged = "user.role_changed";
    public const string UserAccountDeleted = "user.account_deleted";

    public const string PurchaseCreated = "purchase.created";
    public const string PurchaseSucceeded = "purchase.succeeded";
    public const string PurchaseRefunded = "purchase.refunded";

    public const string ProgramAccessGranted = "program_access.granted";
    public const string ProgramAccessRevoked = "program_access.revoked";

    public const string PaymentWebhookProcessed = "payment.webhook_processed";

    public const string ProgramOfferCreated = "program_offer.created";
    public const string ProgramOfferPriceChanged = "program_offer.price_changed";
    public const string ProgramOfferActivated = "program_offer.activated";
    public const string ProgramOfferDeactivated = "program_offer.deactivated";

    public const string QuestionnaireSubmitted = "questionnaire.submitted";
    public const string QuestionnaireRead = "questionnaire.read";

    public const string GuidancePublished = "guidance.published";
    public const string ContentPublished = "content.published";

    public const string EventPublished = "event.published";
    public const string EventCanceled = "event.canceled";

    public const string ChatMessageModerated = "chat.message_moderated";
    public const string ChatUserMuted = "chat.user_muted";
}
