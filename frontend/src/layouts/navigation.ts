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
 * Expert/admin navigation (§45): Dashboard, Programs, Questionnaires, Events, Community,
 * Subscribers, Billing, Notifications, Audit, Settings.
 */
export const ADMIN_NAV_ITEMS: NavItem[] = [
  { key: "dashboard", path: "/admin" },
  { key: "programs", path: "/admin/programs" },
  { key: "questionnaires", path: "/admin/questionnaires" },
  { key: "events", path: "/admin/events" },
  { key: "community", path: "/admin/community" },
  { key: "subscribers", path: "/admin/subscribers" },
  { key: "billing", path: "/admin/billing" },
  { key: "notifications", path: "/admin/notifications" },
  { key: "audit", path: "/admin/audit" },
  { key: "settings", path: "/admin/settings" },
];
