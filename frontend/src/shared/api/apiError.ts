export interface FieldError {
  field: string;
  messageKey: string;
}

/**
 * Mirrors the backend's standard error contract (docs/ARCHITECTURE.md §24,
 * `BUnited.BuildingBlocks.Observability.ErrorHandling.ErrorResponse`). `messageKey` is always a
 * localization key, never a raw server-facing string — components must resolve it via `t()`,
 * never render `message`/`code` directly to the user.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly messageKey: string;
  readonly correlationId: string;
  readonly fieldErrors: readonly FieldError[];

  constructor(status: number, code: string, messageKey: string, correlationId: string, fieldErrors: readonly FieldError[] = []) {
    super(`API error ${code} (status ${status})`);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.messageKey = messageKey;
    this.correlationId = correlationId;
    this.fieldErrors = fieldErrors;
  }

  static isApiError(error: unknown): error is ApiError {
    return error instanceof ApiError;
  }
}

/** A transport-level failure (offline, DNS, CORS) — distinct from a server-returned error. */
export class NetworkError extends Error {
  constructor(cause: unknown) {
    super("Network request failed");
    this.name = "NetworkError";
    this.cause = cause;
  }
}
