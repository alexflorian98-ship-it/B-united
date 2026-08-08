import "@testing-library/jest-dom/vitest";
import i18n from "i18next";

// jsdom doesn't implement <dialog>.showModal()/close() (throws "not implemented") — polyfill the
// minimum behavior our Modal component and tests rely on: toggling the `open` attribute, and
// closing on Escape (which real browsers do natively for a modal dialog).
if (typeof HTMLDialogElement !== "undefined" && !HTMLDialogElement.prototype.showModal) {
  HTMLDialogElement.prototype.showModal = function showModal(this: HTMLDialogElement) {
    this.setAttribute("open", "");
    this.focus();
    this.addEventListener(
      "keydown",
      (event) => {
        if (event.key === "Escape") {
          this.close();
        }
      },
      { once: true },
    );
  };
  HTMLDialogElement.prototype.close = function close(this: HTMLDialogElement) {
    this.removeAttribute("open");
    this.dispatchEvent(new Event("close"));
  };
}
import { initReactI18next } from "react-i18next";
import authEn from "./locales/en/auth.json";
import commonEn from "./locales/en/common.json";
import dashboardEn from "./locales/en/dashboard.json";
import profileEn from "./locales/en/profile.json";
import authRo from "./locales/ro/auth.json";
import commonRo from "./locales/ro/common.json";
import dashboardRo from "./locales/ro/dashboard.json";
import profileRo from "./locales/ro/profile.json";

// A separate, test-only i18next instance: resources are imported statically instead of via the
// production lazy-loading backend (shared/i18n/i18n.ts), so tests get real translated strings
// synchronously — no Suspense/async waiting required to assert on rendered text.
void i18n.use(initReactI18next).init({
  lng: "en",
  fallbackLng: "en",
  ns: ["common", "auth", "dashboard", "profile"],
  defaultNS: "common",
  resources: {
    en: { common: commonEn, auth: authEn, dashboard: dashboardEn, profile: profileEn },
    ro: { common: commonRo, auth: authRo, dashboard: dashboardRo, profile: profileRo },
  },
  interpolation: { escapeValue: false },
  react: { useSuspense: false },
});
