/** Mirrors `BUnited.Modules.Identity.Domain.WellKnownPermissions` — the canonical permission-key
 * list. UI guards built on these are convenience/UX only; the API independently re-checks every
 * one server-side (docs/DEVELOPMENT_INSTRUCTIONS.md §6). */
export const WellKnownPermissions = {
  ContentView: "content.view",
  ContentCreate: "content.create",
  ContentEdit: "content.edit",
  ContentPublish: "content.publish",
  QuestionnaireSubmit: "questionnaire.submit",
  QuestionnaireReview: "questionnaire.review",
  QuestionnaireAnswer: "questionnaire.answer",
  EventsView: "events.view",
  EventsManage: "events.manage",
  ChatUse: "chat.use",
  ChatModerate: "chat.moderate",
  BillingView: "billing.view",
  BillingManage: "billing.manage",
  BillingViewRawWebhookPayloads: "billing.view_raw_webhook_payloads",
  UsersManage: "users.manage",
  AuditView: "audit.view",
} as const;
