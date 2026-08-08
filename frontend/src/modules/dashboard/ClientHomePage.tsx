import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Card } from "../../shared/design-system/Card";
import { StatusBadge } from "../../shared/design-system/StatusBadge";
import { useCurrentUser } from "../../shared/auth/useCurrentUser";

/**
 * Phase 1's client home screen: an account greeting/state and a link to the one destination
 * that actually exists yet (Profile). Deliberately does not show progress/program/event
 * summaries — those modules don't exist until Phase 2+, and inventing placeholder data for
 * them would violate "no placeholder business data" (P1.41.c).
 */
export function ClientHomePage() {
  const { t } = useTranslation(["dashboard", "common"]);
  const user = useCurrentUser();

  return (
    <div className="flex flex-col gap-4 p-4">
      <Card>
        <h1 className="text-lg font-semibold text-text-primary">{t("dashboard:greeting", { email: user?.email ?? "" })}</h1>
        <div className="mt-2">
          <StatusBadge status="success" label={t("dashboard:accountVerified")} />
        </div>
        <p className="mt-3 text-sm text-text-secondary">{t("dashboard:phase1Notice")}</p>
        <Link to="/profile" className="mt-3 inline-block text-sm font-medium text-primary hover:underline">
          {t("common:nav.profile")}
        </Link>
      </Card>
    </div>
  );
}
