/**
 * Release a response whose body we are not going to read. A fetch response holds its connection
 * until the body is consumed or cancelled; the browser allows six connections per host, so leaving
 * bodies unread stalls every later request on the page. The alumni record page leaked one on every
 * 404 — the ordinary case for a student who has not graduated — and the screen never settled.
 */
export async function release(response: Response): Promise<Response> {
  try { await response.body?.cancel(); } catch { /* already consumed or aborted */ }
  return response;
}

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly payload?: unknown
  ) {
    super(message);
  }
}

/**
 * A response body with no sentence in it. Rather than showing a bare status code, say what that
 * code means for the person reading it. The one that matters most is 502/503: the register is
 * restarting — which is exactly what it does for a few seconds once, immediately after the
 * installation wizard finishes — and the only useful instruction is to wait and try again.
 */
function whenTheServerSaysNothing(status: number): string {
  if (status === 0) return "The register did not answer. Check that it is running, then try again.";
  if (status === 502 || status === 503 || status === 504)
    return "The register is not answering yet. It restarts for a few seconds after an installation or an upgrade; wait a moment and try again.";
  if (status === 401) return "Your session has ended. Sign in again.";
  if (status === 403) return "You do not have permission to do that.";
  if (status === 404) return "That record no longer exists.";
  if (status === 429) return "Too many requests from this connection. Wait a minute and try again.";
  return `The register refused the request (${status}).`;
}

/**
 * fetch throws rather than answering when the connection itself fails — the API restarting, the
 * network gone. Turn that into an ApiError like any other, so every caller has one thing to catch
 * and one message to show instead of the browser's "Failed to fetch".
 */
async function send(path: string, init: RequestInit): Promise<Response> {
  try {
    return await fetch(path, init);
  } catch (reason) {
    if (reason instanceof DOMException && reason.name === "AbortError") throw reason;
    throw new ApiError(whenTheServerSaysNothing(0), 0, undefined);
  }
}

let refreshing: Promise<boolean> | undefined;

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const setupToken = typeof window !== "undefined" && (path.startsWith("/api/wizard") || path.startsWith("/api/setup"))
    ? window.sessionStorage.getItem("aegiscore_setup_token")
    : null;
  const headers = new Headers(init?.headers);
  if (setupToken) headers.set("X-Setup-Token", setupToken);
  if (!(init?.body instanceof FormData) && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  let response = await send(path, { credentials: "include", ...init, headers });
  if (response.status === 401 && !path.startsWith("/api/auth/")) {
    refreshing ??= fetch("/api/auth/refresh", { method: "POST", credentials: "include", headers: { "Content-Type": "application/json" }, body: "{}" })
      .then(async result => { await release(result); return result.ok; }).catch(() => false).finally(() => { refreshing = undefined; });
    if (await refreshing) response = await send(path, { credentials: "include", ...init, headers });
  }
  const contentType = response.headers.get("content-type") ?? "";
  const payload = contentType.includes("json")
    ? await response.json()
    : await response.text();
  if (!response.ok) {
    const body = payload as {
      error?: string | { userMessage?: string };
      userMessage?: string;
      message?: string;
      detail?: string;
      errors?: string[];
    };
    // The API closes the register to an administrator without an authenticator app; take them to
    // the page that sets one up rather than showing the same refusal on every screen.
    if (response.status === 403 && (body as { code?: string })?.code === "mfa_required"
      && typeof window !== "undefined" && !window.location.pathname.startsWith("/account/security")) {
      window.location.assign("/account/security");
    }
    const message =
      (typeof body?.error === "string" ? body.error : body?.error?.userMessage) ??
      body?.userMessage ??
      body?.message ??
      // ASP.NET problem+json puts the sentence in `detail`; the setup gate answers in that shape.
      body?.detail ??
      body?.errors?.join(" ") ??
      whenTheServerSaysNothing(response.status);
    throw new ApiError(message, response.status, payload);
  }
  return payload as T;
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

export function apiPostForm<T>(path: string, body: FormData) {
  return request<T>(path, { method: "POST", body });
}

export function setSetupToken(token: string) {
  window.sessionStorage.setItem("aegiscore_setup_token", token.trim());
}
