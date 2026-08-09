import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { primaryButtonLinkClassName } from "../../shared/design-system/linkAsButton";

export function NotFoundPage() {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-background p-6 text-center">
      <h1 className="text-2xl font-semibold text-text-primary">{t("common:notFound.title")}</h1>
      <p className="max-w-md text-sm text-text-secondary">{t("common:notFound.description")}</p>
      <Link to="/" className={primaryButtonLinkClassName}>
        {t("common:notFound.goHome")}
      </Link>
    </div>
  );
}
