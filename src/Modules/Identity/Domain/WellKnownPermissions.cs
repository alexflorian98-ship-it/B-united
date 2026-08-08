namespace BUnited.Modules.Identity.Domain;

/// <summary>
/// The canonical permission-key list (docs/ARCHITECTURE.md §14 permission matrix). Every
/// authorization policy and seeded <c>RolePermission</c> grant must reference these constants
/// — never an inline string literal (docs/DEVELOPMENT_INSTRUCTIONS.md §3).
/// </summary>
public static class WellKnownPermissions
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

    /// <summary>All permission keys, each paired with a stable seed ID and description.</summary>
    public static readonly IReadOnlyList<(Guid Id, string Key, string Description)> All =
    [
        (Guid.Parse("00000000-0000-0000-0000-000000000201"), ContentView, "View published content."),
        (Guid.Parse("00000000-0000-0000-0000-000000000202"), ContentCreate, "Create programs, sections and content items."),
        (Guid.Parse("00000000-0000-0000-0000-000000000203"), ContentEdit, "Edit existing content."),
        (Guid.Parse("00000000-0000-0000-0000-000000000204"), ContentPublish, "Publish/unpublish/archive content."),
        (Guid.Parse("00000000-0000-0000-0000-000000000205"), QuestionnaireSubmit, "Submit questionnaire responses."),
        (Guid.Parse("00000000-0000-0000-0000-000000000206"), QuestionnaireReview, "Review submitted questionnaires."),
        (Guid.Parse("00000000-0000-0000-0000-000000000207"), QuestionnaireAnswer, "Author expert guidance responses."),
        (Guid.Parse("00000000-0000-0000-0000-000000000208"), EventsView, "View events and own registrations."),
        (Guid.Parse("00000000-0000-0000-0000-000000000209"), EventsManage, "Create and manage events."),
        (Guid.Parse("00000000-0000-0000-0000-000000000210"), ChatUse, "Post and read community chat messages."),
        (Guid.Parse("00000000-0000-0000-0000-000000000211"), ChatModerate, "Moderate community chat (mute/report resolution)."),
        (Guid.Parse("00000000-0000-0000-0000-000000000212"), BillingView, "View billing/subscription state (own, or all for staff)."),
        (Guid.Parse("00000000-0000-0000-0000-000000000213"), BillingManage, "Manage plans and subscriber billing state."),
        (Guid.Parse("00000000-0000-0000-0000-000000000214"), UsersManage, "Manage user accounts and role assignments."),
        (Guid.Parse("00000000-0000-0000-0000-000000000215"), AuditView, "Read audit log entries."),
        (Guid.Parse("00000000-0000-0000-0000-000000000216"), BillingViewRawWebhookPayloads, "View raw payment-webhook payloads (technical administrators only, P3.22)."),
    ];
}
