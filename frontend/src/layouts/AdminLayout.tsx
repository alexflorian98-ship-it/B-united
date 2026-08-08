import { useState, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { NavLink } from "react-router-dom";
import { ADMIN_NAV_ITEMS } from "./navigation";

export interface AdminLayoutProps {
  children: ReactNode;
}

const linkClassName = ({ isActive }: { isActive: boolean }) =>
  `rounded-md px-3 py-2 text-sm font-medium ${
    isActive ? "bg-background text-primary" : "text-text-secondary hover:bg-background"
  }`;

/**
 * Expert/admin layout shell (§45): a persistent sidebar from `tablet` up; below that, a
 * hamburger-triggered slide-in drawer, since a 10-item nav can't reasonably live in a bottom
 * bar the way the 5-item client nav does.
 */
export function AdminLayout({ children }: AdminLayoutProps) {
  const { t } = useTranslation();
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);

  const navLinks = ADMIN_NAV_ITEMS.map((item) => (
    <NavLink
      key={item.key}
      to={item.path}
      end={item.path === "/admin"}
      onClick={() => setIsDrawerOpen(false)}
      className={linkClassName}
    >
      {t(`nav.${item.key}`)}
    </NavLink>
  ));

  return (
    <div className="flex min-h-screen bg-background">
      <nav
        aria-label={t("nav.adminNavigation")}
        className="hidden w-56 shrink-0 flex-col gap-1 border-r border-border-default bg-surface p-4 tablet:flex"
      >
        {navLinks}
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
            <span aria-hidden="true">☰</span>
          </button>
        </div>

        <main className="flex-1 p-4">{children}</main>
      </div>

      {isDrawerOpen && (
        <div className="fixed inset-0 z-50 flex tablet:hidden">
          {/* Click-to-dismiss backdrop only — not a tab stop. Keyboard dismissal goes through
              the explicit close button below, so there is exactly one "Close menu" control. */}
          <div
            aria-hidden="true"
            onClick={() => setIsDrawerOpen(false)}
            className="fixed inset-0 bg-text-primary/30"
          />
          <nav
            aria-label={t("nav.adminNavigation")}
            className="relative flex w-64 flex-col gap-1 bg-surface p-4"
          >
            <button
              type="button"
              onClick={() => setIsDrawerOpen(false)}
              aria-label={t("nav.closeMenu")}
              className="mb-2 flex min-h-11 min-w-11 items-center justify-center self-end rounded-md text-text-primary"
            >
              <span aria-hidden="true">×</span>
            </button>
            {navLinks}
          </nav>
        </div>
      )}
    </div>
  );
}
