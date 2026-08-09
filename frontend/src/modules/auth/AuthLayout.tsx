import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { BrandMark } from "../../shared/design-system/BrandMark";
import { Card } from "../../shared/design-system/Card";

export interface AuthLayoutProps {
  children: ReactNode;
}

/**
 * Shared editorial split-screen shell for every auth screen (login, register, verify,
 * password reset): a deep-green brand panel on `tablet`+, a centered form card everywhere.
 * Kept in `modules/auth` (not `shared`) since it's specific to this module's screens.
 */
export function AuthLayout({ children }: AuthLayoutProps) {
  const { t } = useTranslation(["auth", "common"]);

  return (
    <div className="flex min-h-screen flex-col bg-background tablet:flex-row">
      <div className="hidden flex-col justify-between bg-primary p-10 text-on-primary tablet:flex tablet:w-[42%]">
        <BrandMark tone="on-dark" appName={t("common:app.name")} />
        <div className="max-w-sm">
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-accent">{t("auth:panel.eyebrow")}</p>
          <p className="mt-4 font-serif text-2xl font-medium leading-snug">{t("auth:panel.tagline")}</p>
        </div>
        <p className="text-xs text-on-primary/50">© {new Date().getFullYear()} {t("common:app.name")}</p>
      </div>

      <div className="flex flex-1 flex-col items-center justify-center p-6">
        <div className="mb-6 tablet:hidden">
          <BrandMark tone="on-light" appName={t("common:app.name")} />
        </div>
        <Card className="w-full max-w-sm border-border-default shadow-md">{children}</Card>
      </div>
    </div>
  );
}
