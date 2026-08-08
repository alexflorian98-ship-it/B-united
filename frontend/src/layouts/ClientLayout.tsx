import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { NavLink } from "react-router-dom";
import { CLIENT_MOBILE_NAV_KEYS, CLIENT_NAV_ITEMS } from "./navigation";

export interface ClientLayoutProps {
  children: ReactNode;
}

const linkClassName = ({ isActive }: { isActive: boolean }) =>
  `rounded-md px-3 py-2 text-sm font-medium ${
    isActive ? "bg-background text-primary" : "text-text-secondary hover:bg-background"
  }`;

/**
 * Client layout shell (§40): a full nav sidebar from `tablet` up, a bottom nav restricted to
 * the mobile-priority subset below `tablet`. `main` gets bottom padding on mobile so content
 * never sits under the fixed bottom nav.
 */
export function ClientLayout({ children }: ClientLayoutProps) {
  const { t } = useTranslation();
  const mobileItems = CLIENT_NAV_ITEMS.filter((item) => CLIENT_MOBILE_NAV_KEYS.includes(item.key));

  return (
    <div className="flex min-h-screen flex-col bg-background tablet:flex-row">
      <nav
        aria-label={t("nav.mainNavigation")}
        className="hidden shrink-0 border-r border-border-default bg-surface p-4 tablet:flex tablet:w-56 tablet:flex-col tablet:gap-1"
      >
        {CLIENT_NAV_ITEMS.map((item) => (
          <NavLink key={item.key} to={item.path} end={item.path === "/"} className={linkClassName}>
            {t(`nav.${item.key}`)}
          </NavLink>
        ))}
      </nav>

      <main className="flex-1 p-4 pb-20 tablet:pb-4">{children}</main>

      <nav
        aria-label={t("nav.mainNavigation")}
        className="fixed inset-x-0 bottom-0 flex justify-around border-t border-border-default bg-surface py-1 tablet:hidden"
      >
        {mobileItems.map((item) => (
          <NavLink
            key={item.key}
            to={item.path}
            end={item.path === "/"}
            className={({ isActive }) =>
              `flex min-h-11 min-w-11 flex-col items-center justify-center gap-0.5 rounded-md px-2 text-xs font-medium ${
                isActive ? "text-primary" : "text-text-secondary"
              }`
            }
          >
            {t(`nav.${item.key}`)}
          </NavLink>
        ))}
      </nav>
    </div>
  );
}
