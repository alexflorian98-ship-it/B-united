import type { ReactNode } from "react";

export type AlertTone = "success" | "warning" | "danger" | "info";

export interface AlertProps {
  tone: AlertTone;
  title: string;
  children?: ReactNode;
  onDismiss?: () => void;
  /** Localized accessible label for the dismiss button — required whenever `onDismiss` is
   * provided, since this component takes no i18n dependency of its own. */
  dismissLabel?: string;
}

const toneClasses: Record<AlertTone, string> = {
  success: "border-success text-success",
  warning: "border-warning text-warning",
  danger: "border-danger text-danger",
  info: "border-info text-info",
};

export function Alert({ tone, title, children, onDismiss, dismissLabel }: AlertProps) {
  return (
    <div
      role="alert"
      className={`flex items-start justify-between gap-3 rounded-md border bg-surface p-4 ${toneClasses[tone]}`}
    >
      <div>
        <p className="text-sm font-semibold">{title}</p>
        {children && <div className="mt-1 text-sm text-text-secondary">{children}</div>}
      </div>
      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          aria-label={dismissLabel}
          className="rounded-md p-1 text-text-muted hover:text-text-primary"
        >
          ×
        </button>
      )}
    </div>
  );
}
