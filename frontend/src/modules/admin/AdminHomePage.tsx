import { useTranslation } from "react-i18next";
import { Card } from "../../shared/design-system/Card";
import { useCurrentUser } from "../../shared/auth/useCurrentUser";

/** Phase 1's admin/expert home shell — honest about there being no admin functionality yet
 * (that lands starting Phase 2, see docs/TASKS.md). */
export function AdminHomePage() {
  const { t } = useTranslation(["dashboard", "common"]);
  const user = useCurrentUser();

  return (
    <div className="flex flex-col gap-4 p-4">
      <Card>
        <h1 className="text-lg font-semibold text-text-primary">{t("dashboard:greeting", { email: user?.email ?? "" })}</h1>
        <p className="mt-3 text-sm text-text-secondary">{t("dashboard:adminPhase1Notice")}</p>
      </Card>
    </div>
  );
}
