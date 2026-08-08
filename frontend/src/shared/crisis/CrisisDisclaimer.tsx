import { useTranslation } from "react-i18next";
import { Alert } from "../design-system/Alert";

/**
 * docs/PROMPT.md §36: no automated clinical-risk classification anywhere — instead, a
 * localized safety/disclaimer notice and visible emergency information wherever the platform
 * gets close to psychology-adjacent content (questionnaires, guidance). Purely informational;
 * never gates or blocks any action.
 */
export function CrisisDisclaimer() {
  const { t } = useTranslation("questionnaire");

  return (
    <Alert tone="info" title={t("crisis.disclaimerTitle")}>
      <p>{t("crisis.disclaimerBody")}</p>
      <p className="mt-2 font-medium">{t("crisis.emergencyNotice")}</p>
    </Alert>
  );
}
