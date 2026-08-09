import { WellKnownPermissions } from "../shared/permissions/wellKnownPermissions";

export interface NavItem {
  /** Suffix under the "nav." i18n key, e.g. "home" -> t("nav.home"). */
  key: string;
  path: string;
}

/** Client navigation (§40): Home, Programs, Events, Community, My Guidance, Billing, Profile. */
export const CLIENT_NAV_ITEMS: NavItem[] = [
  { key: "home", path: "/" },
  { key: "programs", path: "/programs" },
  { key: "events", path: "/events" },
  { key: "community", path: "/community" },
  { key: "guidance", path: "/guidance" },
  { key: "billing", path: "/billing" },
  { key: "profile", path: "/profile" },
];

/** Mobile bottom-nav priority subset (§40): Home, Programs, Events, Community, Profile. */
export const CLIENT_MOBILE_NAV_KEYS = ["home", "programs", "events", "community", "profile"];

/**
 * Expert/admin navigation (§45, revised by docs/IMPLEMENTATION_PLAN.md Slice A4): Dashboard,
 * Programs, Questionnaires, Events, Community, Clients, Billing, Audit. "Clients" (formerly
 * "Subscribers" — the product no longer sells subscriptions, see Slice A3) is real client
 * administration, not a placeholder. The originally speculative "Notifications" and "Settings"
 * destinations were removed rather than left as permanent placeholders: neither has a real
 * persisted feature behind it (Notifications has no stored history, only a fire-and-forget
 * logging sender; no admin-level settings exist to manage) — Slice A4 is explicit that a
 * placeholder should be removed, not kept, when that's the case.
 */
export const ADMIN_NAV_ITEMS: NavItem[] = [
  { key: "dashboard", path: "/admin" },
  { key: "programs", path: "/admin/programs" },
  { key: "questionnaires", path: "/admin/questionnaires" },
  { key: "events", path: "/admin/events" },
  { key: "community", path: "/admin/community" },
  { key: "clients", path: "/admin/clients" },
  { key: "billing", path: "/admin/billing" },
  { key: "audit", path: "/admin/audit" },
];

/**
 * Permission gate per admin nav destination, mirroring the route-level guards in
 * `app/router.tsx` (which remain the authoritative UX gate; the Api independently re-checks
 * every request, docs/DEVELOPMENT_INSTRUCTIONS.md §6). An empty array means "visible to anyone
 * who can open the `/admin` shell at all" — no destination-specific permission exists yet
 * (`dashboard`).
 */
export const ADMIN_NAV_PERMISSIONS: Record<string, string[]> = {
  dashboard: [],
  programs: [WellKnownPermissions.ContentCreate, WellKnownPermissions.ContentEdit, WellKnownPermissions.ContentPublish],
  questionnaires: [WellKnownPermissions.QuestionnaireReview, WellKnownPermissions.QuestionnaireAnswer],
  events: [WellKnownPermissions.EventsManage],
  community: [WellKnownPermissions.ChatModerate],
  clients: [WellKnownPermissions.UsersManage],
  billing: [WellKnownPermissions.BillingManage],
  audit: [WellKnownPermissions.AuditView],
};

/** All permissions that unlock at least one admin destination — the `/admin` shell itself opens
 * for anyone holding at least one of these (§ "may open when the user holds at least one
 * supported administrative permission"). */
export const ADMIN_SHELL_PERMISSIONS: string[] = Array.from(
  new Set(Object.values(ADMIN_NAV_PERMISSIONS).flat()),
);
