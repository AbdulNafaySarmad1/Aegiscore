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
  try { return localStorage.getItem("aegiscore_refresh_token"); } catch { return null; }
}

function setRefreshToken(token: string) {
  try { localStorage.setItem("aegiscore_refresh_token", token); } catch {}
}

export function clearTokens() {
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
      body: JSON.stringify({ refreshToken: rt }),
    });
    if (!response.ok) { clearTokens(); return null; }
    const data = await response.json();
    setAccessToken(data.accessToken);
    setRefreshToken(data.refreshToken);
    return data.accessToken;
  } catch { clearTokens(); return null; }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getAccessToken();
  const headers: Record<string, string> = {};
  if (init?.body instanceof FormData) {
    Object.assign(headers, init?.headers as Record<string, string> ?? {});
  } else {
    headers["Content-Type"] = "application/json";
    Object.assign(headers, init?.headers as Record<string, string> ?? {});
  }
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const response = await fetch(path, {
    credentials: "include",
    ...init,
    headers,
  });

  if (response.status === 401 && token) {
    const newToken = await tryRefreshToken();
    if (newToken) {
      headers["Authorization"] = `Bearer ${newToken}`;
      const retryResponse = await fetch(path, { ...init, headers, credentials: "include" });
      const ct = retryResponse.headers.get("content-type") ?? "";
      const payload = ct.includes("json")
        ? await retryResponse.json()
        : await retryResponse.text();
      if (!retryResponse.ok) {
        const body = payload as { error?: string | { userMessage?: string }; userMessage?: string; message?: string; errors?: string[] };
        const message = (typeof body?.error === "string" ? body.error : body?.error?.userMessage) ?? body?.userMessage ?? body?.message ?? body?.errors?.join(" ") ?? `Request failed (${retryResponse.status})`;
        throw new ApiError(message, retryResponse.status, payload);
      }
      return payload as T;
    }
    clearTokens();
    if (typeof window !== "undefined" && !window.location.pathname.startsWith("/login"))
      window.location.href = "/login";
    throw new ApiError("Session expired", 401);
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
      errors?: string[];
    };
    const message =
      (typeof body?.error === "string" ? body.error : body?.error?.userMessage) ??
      body?.userMessage ??
      body?.message ??
      body?.errors?.join(" ") ??
      `Request failed (${response.status})`;
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
export { setAccessToken, setRefreshToken };
