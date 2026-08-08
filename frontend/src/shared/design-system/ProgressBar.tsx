export interface ProgressBarProps {
  /** 0-100. */
  value: number;
  /** Accessible label — e.g. "Program progress: 3 of 8 lessons complete". */
  label: string;
}

export function ProgressBar({ value, label }: ProgressBarProps) {
  const clamped = Math.min(100, Math.max(0, value));

  return (
    <div
      role="progressbar"
      aria-label={label}
      aria-valuenow={Math.round(clamped)}
      aria-valuemin={0}
      aria-valuemax={100}
      className="h-2 w-full overflow-hidden rounded-full bg-background"
    >
      <div className="h-full rounded-full bg-primary transition-[width] duration-200" style={{ width: `${clamped}%` }} />
    </div>
  );
}
