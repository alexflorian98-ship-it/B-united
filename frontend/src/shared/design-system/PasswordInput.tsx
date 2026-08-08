import { forwardRef, useId, useState, type InputHTMLAttributes } from "react";

export interface PasswordInputProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
  hint?: string;
  /** Localized accessible label for the show/hide toggle button. */
  toggleVisibilityLabel: string;
}

/** Not a variant of `Input` — the trailing show/hide toggle needs to sit inside the same
 * bordered field, which `Input`'s markup doesn't expose a slot for. */
export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(function PasswordInput(
  { label, error, hint, toggleVisibilityLabel, id, className = "", ...props },
  ref,
) {
  const [visible, setVisible] = useState(false);
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const hintId = hint ? `${inputId}-hint` : undefined;
  const errorId = error ? `${inputId}-error` : undefined;

  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={inputId} className="text-sm font-medium text-text-primary">
        {label}
      </label>
      <div className={`flex items-center rounded-md border ${error ? "border-danger" : "border-border-default"} ${className}`}>
        <input
          ref={ref}
          id={inputId}
          type={visible ? "text" : "password"}
          aria-invalid={error ? true : undefined}
          aria-describedby={[hintId, errorId].filter(Boolean).join(" ") || undefined}
          className="min-w-0 flex-1 rounded-l-md bg-transparent px-3 py-2 text-sm text-text-primary placeholder:text-text-muted disabled:cursor-not-allowed disabled:bg-background disabled:opacity-50"
          {...props}
        />
        <button
          type="button"
          onClick={() => setVisible((current) => !current)}
          aria-label={toggleVisibilityLabel}
          aria-pressed={visible}
          className="flex min-h-11 min-w-11 shrink-0 items-center justify-center rounded-r-md text-text-muted hover:text-text-primary"
        >
          <span aria-hidden="true">{visible ? "🙈" : "👁"}</span>
        </button>
      </div>
      {hint && !error && (
        <p id={hintId} className="text-xs text-text-muted">
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} role="alert" className="text-xs text-danger">
          {error}
        </p>
      )}
    </div>
  );
});
