import i18n from "i18next";
import LanguageDetector from "i18next-browser-languagedetector";
import resourcesToBackend from "i18next-resources-to-backend";
import { initReactI18next } from "react-i18next";

export const SUPPORTED_LANGUAGES = ["ro", "en"] as const;
export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number];

/** Romanian is the product's default UI language (see CLAUDE.md). */
export const DEFAULT_LANGUAGE: SupportedLanguage = "ro";

export const LANGUAGE_STORAGE_KEY = "bunited.language";

void i18n
  // Lazy-loads each namespace as its own dynamic import (Vite code-splits it into a
  // separate chunk), so a route only pays for the namespaces it actually renders.
  .use(
    resourcesToBackend(
      (language: string, namespace: string) => import(`../../locales/${language}/${namespace}.json`),
    ),
  )
  // Only reads a previously persisted choice — deliberately does NOT guess from the
  // browser's Accept-Language, so a first-time visitor always lands on the product's
  // Romanian default rather than an arbitrary detected locale.
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    // No explicit `lng` here: setting one would take priority over LanguageDetector's
    // result and a returning visitor's persisted choice would never be honored (found via
    // testing — a stored "en" preference was silently ignored on reload). `fallbackLng`
    // covers both "nothing detected" and "active language missing a key", and doubles as
    // the initial default for first-time visitors, which is why it's "ro" and not "en".
    fallbackLng: DEFAULT_LANGUAGE,
    supportedLngs: SUPPORTED_LANGUAGES,
    ns: ["common"],
    defaultNS: "common",
    detection: {
      order: ["localStorage"],
      caches: ["localStorage"],
      lookupLocalStorage: LANGUAGE_STORAGE_KEY,
    },
    interpolation: {
      escapeValue: false,
    },
    react: {
      useSuspense: true,
    },
  });

// Keep <html lang="..."> in sync with the active language (accessibility/SEO) — i18next does
// not do this itself.
const syncDocumentLanguage = (language: string) => {
  document.documentElement.lang = language;
};
i18n.on("languageChanged", syncDocumentLanguage);
if (i18n.resolvedLanguage) {
  syncDocumentLanguage(i18n.resolvedLanguage);
}

export default i18n;
