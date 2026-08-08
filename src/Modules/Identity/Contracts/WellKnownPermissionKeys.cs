namespace BUnited.Modules.Identity.Contracts;

/// <summary>
/// The permission-key strings other modules need to reference in `[Authorize(Policy = ...)]`
/// attributes, without referencing Identity's Domain layer (CLAUDE.md: "Never reference another
/// module's Domain or Infrastructure layer"). Values MUST stay identical to
/// <c>BUnited.Modules.Identity.Domain.WellKnownPermissions</c> — the Domain type stays the
/// single place a policy is actually registered/granted; this is only the cross-module-safe
/// mirror of the string values, kept honest by
/// <c>IdentityAuthorizationExtensionsTests.WellKnownPermissionKeys_mirrors_the_domain_permission_set_exactly</c>.
/// </summary>
public static class WellKnownPermissionKeys
{
    public const string ContentView = "content.view";
    public const string ContentCreate = "content.create";
    public const string ContentEdit = "content.edit";
    public const string ContentPublish = "content.publish";

    public const string QuestionnaireSubmit = "questionnaire.submit";
    public const string QuestionnaireReview = "questionnaire.review";
    public const string QuestionnaireAnswer = "questionnaire.answer";

    public const string EventsView = "events.view";
    public const string EventsManage = "events.manage";

    public const string ChatUse = "chat.use";
    public const string ChatModerate = "chat.moderate";

    public const string BillingView = "billing.view";
    public const string BillingManage = "billing.manage";
    public const string BillingViewRawWebhookPayloads = "billing.view_raw_webhook_payloads";

    public const string UsersManage = "users.manage";

    public const string AuditView = "audit.view";
}
