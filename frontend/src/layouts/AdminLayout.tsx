import { useState, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { NavLink } from "react-router-dom";
import { useAuthStore } from "../shared/auth/authStore";
import { BrandMark } from "../shared/design-system/BrandMark";
import { Icon, type IconName } from "../shared/design-system/Icon";
import { ADMIN_NAV_ITEMS, ADMIN_NAV_PERMISSIONS } from "./navigation";

export interface AdminLayoutProps {
  children: ReactNode;
}

const navIconByKey: Record<string, IconName> = {
  dashboard: "admin",
  programs: "programs",
  questionnaires: "questionnaire",
  events: "events",
  community: "community",
  clients: "subscribers",
  billing: "billing",
  audit: "audit",
};

const linkClassName = ({ isActive }: { isActive: boolean }) =>
  `flex items-center gap-3 rounded-full px-4 py-2.5 text-sm font-medium transition-colors duration-150 ${
    isActive ? "bg-on-primary/12 text-on-primary" : "text-on-primary/70 hover:bg-on-primary/8 hover:text-on-primary"
  }`;

/**
 * Expert/admin layout shell (§45): a persistent dark-green sidebar from `tablet` up; below that,
 * a hamburger-triggered slide-in drawer, since a 10-item nav can't reasonably live in a bottom
 * bar the way the 5-item client nav does.
 */
export function AdminLayout({ children }: AdminLayoutProps) {
  const { t } = useTranslation();
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  // Select the stable `user` object, not a derived `.permissions ?? []` — a selector that
  // returns a fresh array literal on every call breaks zustand's useSyncExternalStore identity
  // check and causes an infinite render loop.
  const user = useAuthStore((state) => state.user);
  const userPermissions = user?.permissions ?? [];

  const visibleNavItems = ADMIN_NAV_ITEMS.filter((item) => {
    const requiredPermissions = ADMIN_NAV_PERMISSIONS[item.key] ?? [];
    return requiredPermissions.length === 0 || requiredPermissions.some((permission) => userPermissions.includes(permission));
  });

  const navLinks = visibleNavItems.map((item) => (
    <NavLink
      key={item.key}
      to={item.path}
      end={item.path === "/admin"}
      onClick={() => setIsDrawerOpen(false)}
      className={linkClassName}
    >
      <Icon name={navIconByKey[item.key] ?? "admin"} />
      {t(`nav.${item.key}`)}
    </NavLink>
  ));
  const clientLink = (
    <NavLink to="/" onClick={() => setIsDrawerOpen(false)} className={`${linkClassName({ isActive: false })} border border-on-primary/20`}>
      <Icon name="home" />
      {t("nav.backToApp")}
    </NavLink>
  );

  return (
    <div className="flex min-h-screen bg-background">
      <nav
        aria-label={t("nav.adminNavigation")}
        className="hidden h-screen w-64 shrink-0 flex-col bg-primary p-5 tablet:sticky tablet:top-0 tablet:flex tablet:self-start"
      >
        <div className="mb-8 px-1">
          <BrandMark tone="on-dark" appName={t("app.name")} />
        </div>
        <div className="flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto">{navLinks}</div>
        <div className="mt-4 shrink-0">{clientLink}</div>
      </nav>

      <div className="flex flex-1 flex-col">
        <div className="flex items-center border-b border-border-default bg-surface p-2 tablet:hidden">
          <button
            type="button"
            onClick={() => setIsDrawerOpen(true)}
            aria-label={t("nav.openMenu")}
            aria-expanded={isDrawerOpen}
            className="flex min-h-11 min-w-11 items-center justify-center rounded-md text-text-primary"
          >
            <Icon name="menu" />
          </button>
          <BrandMark tone="on-light" appName={t("app.name")} />
        </div>

        <main className="flex-1 p-4 tablet:p-8">{children}</main>
      </div>

      {isDrawerOpen && (
        <div className="fixed inset-0 z-50 flex tablet:hidden">
          {/* Click-to-dismiss backdrop only — not a tab stop. Keyboard dismissal goes through
              the explicit close button below, so there is exactly one "Close menu" control. */}
          <div
            aria-hidden="true"
            onClick={() => setIsDrawerOpen(false)}
            className="fixed inset-0 bg-text-primary/40"
          />
          <nav
            aria-label={t("nav.adminNavigation")}
            className="relative flex h-full w-72 flex-col bg-primary p-5"
          >
            <button
              type="button"
              onClick={() => setIsDrawerOpen(false)}
              aria-label={t("nav.closeMenu")}
              className="mb-2 flex min-h-11 min-w-11 items-center justify-center self-end rounded-md text-on-primary"
            >
              <Icon name="close" />
            </button>
            <div className="flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto">{navLinks}</div>
            <div className="mt-4 shrink-0">{clientLink}</div>
          </nav>
        </div>
      )}
    </div>
  );
}
