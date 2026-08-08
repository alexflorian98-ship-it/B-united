import type { ReactNode } from "react";

export type ToastTone = "success" | "warning" | "danger" | "info";

export interface ToastProps {
  tone?: ToastTone;
  children: ReactNode;
  onDismiss: () => void;
}

const toneClasses: Record<ToastTone, string> = {
  success: "border-success",
  warning: "border-warning",
  danger: "border-danger",
  info: "border-info",
};

export function Toast({ tone = "info", children, onDismiss }: ToastProps) {
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
        aria-label="Dismiss"
        className="rounded-md p-1 text-text-muted hover:text-text-primary"
      >
        ×
      </button>
    </div>
  );
}
