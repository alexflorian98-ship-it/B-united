import { ApiError, NetworkError, type FieldError } from "./apiError";

type AccessTokenProvider = () => string | null;
type UnauthorizedHandler = () => Promise<string | null>;

let accessTokenProvider: AccessTokenProvider = () => null;
let unauthorizedHandler: UnauthorizedHandler | null = null;
let refreshInFlight: Promise<string | null> | null = null;

/**
 * Wires the API client to the session layer without a circular import between `shared/api` and
 * `shared/auth` — `SessionProvider` calls this once at startup. `onUnauthorized` is invoked at
 * most once per burst of concurrent 401s (see `refreshInFlight`), satisfying the "single retry
 * with concurrent-refresh deduplication" requirement (P1.37.b) at the transport layer, not in
 * every caller.
 */
export function configureApiClient(options: { getAccessToken: AccessTokenProvider; onUnauthorized?: UnauthorizedHandler }) {
  accessTokenProvider = options.getAccessToken;
  unauthorizedHandler = options.onUnauthorized ?? null;
}

export interface ApiRequestOptions {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  signal?: AbortSignal;
  /** Skip attaching a bearer token and skip the 401-refresh dance (login/register/refresh itself). */
  skipAuth?: boolean;
}

interface RawErrorPayload {
  code?: string;
  messageKey?: string;
  correlationId?: string;
  errors?: FieldError[];
}

async function rawRequest(path: string, options: ApiRequestOptions, accessTokenOverride?: string | null): Promise<Response> {
  const baseUrl = import.meta.env.VITE_API_BASE_URL as string;
  const headers: Record<string, string> = { "Content-Type": "application/json" };

  if (!options.skipAuth) {
    const token = accessTokenOverride !== undefined ? accessTokenOverride : accessTokenProvider();
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
  }

  try {
    return await fetch(`${baseUrl}${path}`, {
      method: options.method ?? "GET",
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
      signal: options.signal,
    });
  } catch (cause) {
    throw new NetworkError(cause);
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  let payload: RawErrorPayload | null = null;
  try {
    payload = (await response.json()) as RawErrorPayload;
  } catch {
    // No/invalid JSON body — fall through to the generic error below.
  }

  return new ApiError(
    response.status,
    payload?.code ?? "UNKNOWN_ERROR",
    payload?.messageKey ?? "errors.internalServerError",
    payload?.correlationId ?? "",
    payload?.errors ?? [],
  );
}

async function refreshAccessTokenOnce(): Promise<string | null> {
  if (!unauthorizedHandler) {
    return null;
  }

  refreshInFlight ??= unauthorizedHandler().finally(() => {
    refreshInFlight = null;
  });

  return refreshInFlight;
}

/** Every non-2xx response is turned into a thrown `ApiError` before any caller sees `data`. */
export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  let response = await rawRequest(path, options);

  if (response.status === 401 && !options.skipAuth) {
    const refreshedToken = await refreshAccessTokenOnce();
    if (refreshedToken) {
      response = await rawRequest(path, options, refreshedToken);
    }
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
