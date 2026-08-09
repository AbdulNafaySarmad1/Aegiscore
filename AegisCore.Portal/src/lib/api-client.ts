// ─── AegisCore API Client ─── src/lib/api-client.ts ───
// Single browser-side fetch foundation. All module code must go through here.
//
// Semantics (per docs/FRONTEND_API_HANDOFF.md):
// - Same-origin /api/* requests carry HttpOnly cookies (credentials: "include").
// - 401 on a non-auth endpoint triggers ONE atomic refresh (POST /api/auth/refresh {})
//   then a single retry. Refresh rotation/reuse failures surface as 401.
// - A terminal 401 dispatches "aegiscore:auth-expired" so the session provider
//   can drop the local session and redirect to /login.
// - 403 is NOT retried and NOT conflated with 401: it throws a forbidden ApiError.

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly payload?: unknown
  ) {
    super(message);
    this.name = "ApiError";
  }
  get isUnauthorized() { return this.status === 401; }
  get isForbidden() { return this.status === 403; }
  get isValidation() { return this.status === 400; }
  get isRateLimited() { return this.status === 429; }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

export const AUTH_EXPIRED_EVENT = "aegiscore:auth-expired";

function notifyAuthExpired() {
  if (typeof window !== "undefined") {
    window.dispatchEvent(new CustomEvent(AUTH_EXPIRED_EVENT));
  }
}

// Single-flight refresh: concurrent 401s share one rotation request.
let refreshInFlight: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  refreshInFlight ??= (async () => {
    try {
      const response = await fetch("/api/auth/refresh", {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: "{}"
      });
      return response.ok;
    } catch {
      return false;
    } finally {
      // Allow the next 401 to attempt a fresh rotation.
      setTimeout(() => { refreshInFlight = null; }, 0);
    }
  })();
  return refreshInFlight;
}

type ErrorBody = {
  error?: string | { userMessage?: string };
  userMessage?: string;
  message?: string;
  title?: string;
  errors?: string[] | Record<string, string[]>;
};

function extractMessage(payload: unknown, status: number): string {
  const body = payload as ErrorBody | null;
  const errors = body?.errors;
  const joined = Array.isArray(errors)
    ? errors.join(" ")
    : errors && typeof errors === "object"
      ? Object.values(errors).flat().join(" ")
      : undefined;
  return (
    (typeof body?.error === "string" ? body.error : body?.error?.userMessage) ??
    body?.userMessage ??
    body?.message ??
    body?.title ??
    joined ??
    (typeof payload === "string" && payload.length < 300 ? payload : undefined) ??
    `Request failed (${status})`
  );
}

async function execute<T>(path: string, init?: RequestInit): Promise<{ response: Response; payload: T }> {
  const response = await fetch(path, {
    credentials: "include",
    ...init,
    headers: init?.body instanceof FormData
      ? init.headers
      : { "Content-Type": "application/json", ...init?.headers }
  });
  const contentType = response.headers.get("content-type") ?? "";
  const payload = (contentType.includes("json")
    ? await response.json().catch(() => null)
    : await response.text().catch(() => "")) as T;
  return { response, payload };
}

const isAuthEndpoint = (path: string) => path.startsWith("/api/auth/");

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let { response, payload } = await execute<T>(path, init);

  // Transparent access-token refresh for ordinary API calls.
  if (response.status === 401 && !isAuthEndpoint(path)) {
    if (await tryRefresh()) {
      ({ response, payload } = await execute<T>(path, init));
    }
  }

  if (!response.ok) {
    if (response.status === 401 && !isAuthEndpoint(path)) notifyAuthExpired();
    throw new ApiError(extractMessage(payload, response.status), response.status, payload);
  }
  return payload;
}

export function apiGet<T>(path: string, signal?: AbortSignal) {
  return request<T>(path, { signal });
}
export function apiPost<T>(path: string, body?: unknown) {
  return request<T>(path, {
    method: "POST",
    body: body instanceof FormData ? body : JSON.stringify(body ?? {})
  });
}
export function apiPut<T>(path: string, body?: unknown) {
  return request<T>(path, {
    method: "PUT",
    body: JSON.stringify(body ?? {})
  });
}
export function apiDelete<T>(path: string) {
  return request<T>(path, { method: "DELETE" });
}
