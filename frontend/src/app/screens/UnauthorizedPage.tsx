import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { primaryButtonLinkClassName } from "../../shared/design-system/linkAsButton";

/**
 * Reached only if an unhandled 401 slips past a route guard (guards redirect straight to
 * `/login`, which is the actual resolution UI for "not logged in yet") — e.g. a background
 * mutation whose access token expired mid-request and silent refresh also failed.
 */
export function UnauthorizedPage() {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-background p-6 text-center">
      <h1 className="text-2xl font-semibold text-text-primary">{t("common:unauthorized.title")}</h1>
      <p className="max-w-md text-sm text-text-secondary">{t("common:unauthorized.description")}</p>
      <Link to="/login" className={primaryButtonLinkClassName}>
        {t("common:unauthorized.goToLogin")}
      </Link>
    </div>
  );
}
