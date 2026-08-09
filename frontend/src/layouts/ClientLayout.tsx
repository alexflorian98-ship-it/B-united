import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { NavLink } from "react-router-dom";
import { BrandMark } from "../shared/design-system/BrandMark";
import { Icon, type IconName } from "../shared/design-system/Icon";
import { CLIENT_MOBILE_NAV_KEYS, CLIENT_NAV_ITEMS } from "./navigation";
import { useAuthStore } from "../shared/auth/authStore";
import { WellKnownPermissions } from "../shared/permissions/wellKnownPermissions";

export interface ClientLayoutProps {
  children: ReactNode;
}

const navIconByKey: Record<string, IconName> = {
  home: "home",
  programs: "programs",
  events: "events",
  community: "community",
  guidance: "questionnaire",
  billing: "billing",
  profile: "profile",
};

const sidebarLinkClassName = ({ isActive }: { isActive: boolean }) =>
  `flex items-center gap-3 rounded-full px-4 py-2.5 text-sm font-medium transition-colors duration-150 ${
    isActive ? "bg-on-primary/12 text-on-primary" : "text-on-primary/70 hover:bg-on-primary/8 hover:text-on-primary"
  }`;

const mobileLinkClassName = ({ isActive }: { isActive: boolean }) =>
  `flex min-h-11 min-w-11 flex-col items-center justify-center gap-0.5 rounded-lg px-2 text-[11px] font-medium ${
    isActive ? "text-primary" : "text-text-muted"
  }`;

/**
 * Client layout shell (§40): a full dark-green sidebar with brand mark from `tablet` up, a
 * compact icon bottom nav restricted to the mobile-priority subset below `tablet`. `main` gets
 * bottom padding on mobile so content never sits under the fixed bottom nav.
 */
export function ClientLayout({ children }: ClientLayoutProps) {
  const { t } = useTranslation();
  const canOpenAdmin = useAuthStore((state) => state.user?.permissions.includes(WellKnownPermissions.ContentCreate) ?? false);
  const mobileItems = CLIENT_NAV_ITEMS.filter((item) => CLIENT_MOBILE_NAV_KEYS.includes(item.key));

  return (
    <div className="flex min-h-screen flex-col bg-background tablet:flex-row">
      <nav
        aria-label={t("nav.mainNavigation")}
        className="hidden h-screen shrink-0 flex-col bg-primary p-5 tablet:sticky tablet:top-0 tablet:flex tablet:w-64 tablet:self-start"
      >
        <div className="mb-8 px-1">
          <BrandMark tone="on-dark" appName={t("app.name")} />
        </div>
        <div className="flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto">
          {CLIENT_NAV_ITEMS.map((item) => (
            <NavLink key={item.key} to={item.path} end={item.path === "/"} className={sidebarLinkClassName}>
              <Icon name={navIconByKey[item.key] ?? "home"} />
              {t(`nav.${item.key}`)}
            </NavLink>
          ))}
        </div>
        {canOpenAdmin && (
          <div className="mt-4 shrink-0">
            <NavLink to="/admin" className={`${sidebarLinkClassName({ isActive: false })} border border-on-primary/20`}>
              <Icon name="admin" />
              {t("nav.openAdmin")}
            </NavLink>
          </div>
        )}
      </nav>

      <main className="flex-1 p-4 pb-24 tablet:p-8 tablet:pb-8">{children}</main>

      <nav
        aria-label={t("nav.mainNavigation")}
        className="fixed inset-x-0 bottom-0 z-40 flex justify-around border-t border-border-default bg-surface py-1.5 shadow-lg tablet:hidden"
      >
        {mobileItems.map((item) => (
          <NavLink key={item.key} to={item.path} end={item.path === "/"} className={mobileLinkClassName}>
            <Icon name={navIconByKey[item.key] ?? "home"} size={22} />
            {t(`nav.${item.key}`)}
          </NavLink>
        ))}
        {canOpenAdmin && (
          <NavLink to="/admin" className={mobileLinkClassName}>
            <Icon name="admin" size={22} />
            {t("nav.adminShort")}
          </NavLink>
        )}
      </nav>
    </div>
  );
}
