import type { ChangeEvent } from "react";
import { useTranslation } from "react-i18next";
import { SUPPORTED_LANGUAGES, type SupportedLanguage } from "./i18n";

/**
 * Minimal functional switcher — not yet built on formal design-system primitives (those land
 * with P1.29/P1.30). Anonymous users: persisted to localStorage automatically by
 * i18next-browser-languagedetector's `caches` option. Authenticated users: persisting to
 * `UserPreference` needs the profile API (lands with P1.42) — wire that call in here once it
 * exists, alongside the localStorage write.
 */
export function LanguageSwitcher() {
  const { t, i18n } = useTranslation();

  const handleChange = (event: ChangeEvent<HTMLSelectElement>) => {
    const next = event.target.value as SupportedLanguage;
    void i18n.changeLanguage(next);
  };

  return (
    <label className="inline-flex items-center gap-2 text-sm">
      <span className="sr-only">{t("language.ro")} / {t("language.en")}</span>
      <select
        value={i18n.resolvedLanguage}
        onChange={handleChange}
        className="rounded-md border border-border-default bg-surface px-2 py-1 text-text-primary"
      >
        {SUPPORTED_LANGUAGES.map((language) => (
          <option key={language} value={language}>
            {t(`language.${language}`)}
          </option>
        ))}
      </select>
    </label>
  );
}
