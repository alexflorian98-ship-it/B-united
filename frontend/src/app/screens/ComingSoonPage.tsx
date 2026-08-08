import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { EmptyState } from "../../shared/design-system/EmptyState";
import { primaryButtonLinkClassName } from "../../shared/design-system/linkAsButton";

export interface ComingSoonPageProps {
  /** A `common:nav.*` key — reused so the page title always matches the nav label that led here. */
  titleKey: string;
}

/**
 * An honest, deliberate placeholder for a nav destination that's part of the product's full
 * navigation (§40/§45) but hasn't shipped yet in this delivery phase. Never fake business data
 * (no invented programs/events/etc.) — just a clear, translated "not yet" state, so every nav
 * link resolves to something real instead of a dead end (P1.41.c).
 */
export function ComingSoonPage({ titleKey }: ComingSoonPageProps) {
  const { t } = useTranslation("common");

  return (
    <div className="p-4">
      <EmptyState
        title={t(titleKey)}
        description={t("comingSoon.description")}
        action={
          <Link to="/" className={primaryButtonLinkClassName}>
            {t("nav.home")}
          </Link>
        }
      />
    </div>
  );
}
