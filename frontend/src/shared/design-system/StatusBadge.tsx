import { Badge, type BadgeTone } from "./Badge";

export type StatusBadgeStatus = "success" | "warning" | "danger" | "info" | "neutral";

export interface StatusBadgeProps {
  status: StatusBadgeStatus;
  label: string;
}

const toneByStatus: Record<StatusBadgeStatus, BadgeTone> = {
  success: "success",
  warning: "warning",
  danger: "danger",
  info: "info",
  neutral: "neutral",
};

const iconByStatus: Record<StatusBadgeStatus, string> = {
  success: "✓",
  warning: "!",
  danger: "✕",
  info: "i",
  neutral: "•",
};

/** A `Badge` specialized for account/entity state, never relying on color alone — every status
 * pairs a tone with a required text label and a matching icon glyph. */
export function StatusBadge({ status, label }: StatusBadgeProps) {
  return (
    <Badge tone={toneByStatus[status]}>
      <span aria-hidden="true">{iconByStatus[status]}</span>
      <span className="ml-1">{label}</span>
    </Badge>
  );
}
