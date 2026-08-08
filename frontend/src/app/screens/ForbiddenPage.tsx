import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { primaryButtonLinkClassName } from "../../shared/design-system/linkAsButton";

export function ForbiddenPage() {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 p-6 text-center">
      <h1 className="text-xl font-semibold text-text-primary">{t("common:forbidden.title")}</h1>
      <p className="max-w-md text-sm text-text-secondary">{t("common:forbidden.description")}</p>
      <Link to="/" className={primaryButtonLinkClassName}>
        {t("common:forbidden.goHome")}
      </Link>
    </div>
  );
}
