// Must be the first import: sets Zod to jitless mode before any schema anywhere in the app
// (imported transitively below) can run its eval-availability probe. See that module for why.
import "./shared/zodJitlessConfig";
import { StrictMode, Suspense } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import "./shared/i18n/i18n";
import App from "./App";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <Suspense fallback={null}>
      <App />
    </Suspense>
  </StrictMode>,
);
