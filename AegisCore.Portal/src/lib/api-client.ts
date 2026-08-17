export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly payload?: unknown
  ) {
    super(message);
  }
}

function getAccessToken(): string | null {
  if (typeof window === "undefined") return null;
  try { return localStorage.getItem("aegiscore_access_token"); } catch { return null; }
}

function setAccessToken(token: string) {
  try { localStorage.setItem("aegiscore_access_token", token); } catch {}
}

export function getRefreshToken(): string | null {
  if (typeof window === "undefined") return null;
  try { return localStorage.getItem("aegiscore_refresh_token"); } catch { return null; }
}

function setRefreshToken(token: string) {
  try { localStorage.setItem("aegiscore_refresh_token", token); } catch {}
}

export function clearTokens() {
  if (typeof window === "undefined") return;
  try {
    localStorage.removeItem("aegiscore_access_token");
    localStorage.removeItem("aegiscore_refresh_token");
    localStorage.removeItem("aegiscore_demo_session");
  } catch {}
}

async function tryRefreshToken(): Promise<string | null> {
  const rt = getRefreshToken();
  if (!rt) return null;

  try {
    const response = await fetch("/api/auth/refresh", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      credentials: "include",
      body: JSON.stringify({ refreshToken: rt }),
    });

    if (!response.ok) {
      clearTokens();
      return null;
    }

    const data = await response.json() as { accessToken: string; refreshToken: string };
    setAccessToken(data.accessToken);
    setRefreshToken(data.refreshToken);
    return data.accessToken;
  } catch {
    clearTokens();
    return null;
  }
}

function buildHeaders(init: RequestInit | undefined, token: string | null): Headers {
  const headers = new Headers(init?.headers);
  if (!(init?.body instanceof FormData) && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }
  if (token) headers.set("Authorization", `Bearer ${token}`);
  return headers;
}

async function authorizedFetch(path: string, init?: RequestInit): Promise<Response> {
  const token = getAccessToken();
  const response = await fetch(path, {
    ...init,
    credentials: "include",
    headers: buildHeaders(init, token),
  });

  if (response.status !== 401 || !token) return response;

  const newToken = await tryRefreshToken();
  if (newToken) {
    return fetch(path, {
      ...init,
      credentials: "include",
      headers: buildHeaders(init, newToken),
    });
  }

  clearTokens();
  if (typeof window !== "undefined" && !window.location.pathname.startsWith("/login")) {
    window.location.href = "/login";
  }
  throw new ApiError("Session expired", 401);
}

async function parseError(response: Response): Promise<ApiError> {
  const contentType = response.headers.get("content-type") ?? "";
  let payload: unknown;
  try {
    payload = contentType.includes("json") ? await response.json() : await response.text();
  } catch {
    payload = null;
  }

  const body = payload as {
    error?: string | { userMessage?: string };
    userMessage?: string;
    message?: string;
    errors?: string[];
  } | null;

  const message =
    (typeof body?.error === "string" ? body.error : body?.error?.userMessage) ??
    body?.userMessage ??
    body?.message ??
    body?.errors?.join(" ") ??
    `Request failed (${response.status})`;

  return new ApiError(message, response.status, payload);
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await authorizedFetch(path, init);
  if (!response.ok) throw await parseError(response);

  if (response.status === 204) return undefined as T;
  const contentType = response.headers.get("content-type") ?? "";
  return (contentType.includes("json") ? await response.json() : await response.text()) as T;
}

export function apiGet<T>(path: string, signal?: AbortSignal) {
  return request<T>(path, { signal });
}

export function apiPost<T>(path: string, body?: unknown) {
  return request<T>(path, {
    method: "POST",
    body: body instanceof FormData ? body : JSON.stringify(body ?? {}),
  });
}

export function apiPut<T>(path: string, body?: unknown) {
  return request<T>(path, {
    method: "PUT",
    body: body instanceof FormData ? body : JSON.stringify(body ?? {}),
  });
}

export async function apiDownload(path: string, init?: RequestInit, fallbackName = "download") {
  const response = await authorizedFetch(path, init);
  if (!response.ok) throw await parseError(response);

  const blob = await response.blob();
  const disposition = response.headers.get("content-disposition") ?? "";
  const utf8Match = disposition.match(/filename\*=UTF-8''([^;]+)/i);
  const simpleMatch = disposition.match(/filename="?([^";]+)"?/i);
  const fileName = decodeURIComponent(utf8Match?.[1] ?? simpleMatch?.[1] ?? fallbackName);

  const url = URL.createObjectURL(blob);
  try {
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
  } finally {
    window.setTimeout(() => URL.revokeObjectURL(url), 1_000);
  }
}

export { setAccessToken, setRefreshToken };
