import type { FieldValues, Path, UseFormSetError } from "react-hook-form";
import { ApiError } from "../api/apiError";

/** Generic backend error codes (`ErrorResponse.InternalServerError`/etc., BuildingBlocks-owned)
 * live only in `common.json` — every other `messageKey` (auth-specific ones like
 * `errors.invalidCredentials`) lives in the calling page's own namespace and resolves correctly
 * under `useTranslation`'s default namespace without a prefix. Without this, `t()` would look
 * these four up in the wrong namespace and render the raw key (found via live testing — see
 * docs/HANDOVER.md). */
const COMMON_NAMESPACE_ERROR_KEYS = new Set([
  "errors.validationFailed",
  "errors.internalServerError",
  "errors.notFound",
  "errors.rateLimitExceeded",
]);

/** Backend-owned business-rule errors (Billing, Quiz authoring/grading, etc.) can surface from
 * more than one page with a different default namespace (admin offer forms, client checkout,
 * program-detail paywall, the admin quiz builder) — so these live once in `common.json` under
 * `errors.<domain>.*` rather than being duplicated into every consuming namespace. */
const COMMON_NAMESPACE_ERROR_PREFIXES = ["errors.billing.", "errors.quiz.", "errors.quizQuestion.", "errors.quizOption.", "errors.contentItem.", "errors.identity."];

export function resolveMessageKey(messageKey: string): string {
  if (COMMON_NAMESPACE_ERROR_KEYS.has(messageKey) || COMMON_NAMESPACE_ERROR_PREFIXES.some((prefix) => messageKey.startsWith(prefix))) {
    return `common:${messageKey}`;
  }
  return messageKey;
}

/**
 * Maps the backend's field-level validation errors (`ApiError.fieldErrors`) onto the matching
 * React Hook Form field, and returns a translated top-level message for anything that isn't a
 * field error (or doesn't match a known field on this form) for a form-level `Alert`.
 */
export function applyApiErrorToForm<TFieldValues extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<TFieldValues>,
  t: (key: string) => string,
  knownFields: ReadonlyArray<Path<TFieldValues>>,
): string {
  if (!ApiError.isApiError(error)) {
    return t("common:errors.internalServerError");
  }

  let unmatchedFieldErrors = 0;

  for (const fieldError of error.fieldErrors) {
    const field = knownFields.find((known) => known.toLowerCase() === fieldError.field.toLowerCase());
    if (field) {
      setError(field, { type: "server", message: t(fieldError.messageKey) });
    } else {
      unmatchedFieldErrors++;
    }
  }

  if (error.fieldErrors.length > 0 && unmatchedFieldErrors === 0) {
    return t("common:errors.validationFailed");
  }

  return t(resolveMessageKey(error.messageKey));
}
