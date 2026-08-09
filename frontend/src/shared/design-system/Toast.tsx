import type { ReactNode } from "react";

export type ToastTone = "success" | "warning" | "danger" | "info";

export interface ToastProps {
  tone?: ToastTone;
  children: ReactNode;
  onDismiss: () => void;
  /** Localized accessible label for the dismiss button — this component takes no i18n
   * dependency of its own, so the caller must supply already-translated text. */
  dismissLabel: string;
}

const toneClasses: Record<ToastTone, string> = {
  success: "border-success",
  warning: "border-warning",
  danger: "border-danger",
  info: "border-info",
};

export function Toast({ tone = "info", children, onDismiss, dismissLabel }: ToastProps) {
  return (
    <div
      role="status"
      aria-live="polite"
      className={`flex items-center justify-between gap-3 rounded-md border bg-surface p-3 text-sm text-text-primary shadow-lg ${toneClasses[tone]}`}
    >
      <span>{children}</span>
      <button
        type="button"
        onClick={onDismiss}
        aria-label={dismissLabel}
        className="rounded-md p-1 text-text-muted hover:text-text-primary"
      >
        ×
      </button>
    </div>
  );
}
