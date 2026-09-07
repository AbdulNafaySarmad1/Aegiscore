export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly payload?: unknown
  ) {
    super(message);
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
  let response = await fetch(path, {
    credentials: "include",
    ...init,
    headers,
  });
  if (response.status === 401 && !path.startsWith("/api/auth/")) {
    refreshing ??= fetch("/api/auth/refresh", { method: "POST", credentials: "include", headers: { "Content-Type": "application/json" }, body: "{}" })
      .then(result => result.ok).catch(() => false).finally(() => { refreshing = undefined; });
    if (await refreshing) response = await fetch(path, { credentials: "include", ...init, headers });
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

export function apiPostForm<T>(path: string, body: FormData) {
  return request<T>(path, { method: "POST", body });
}

export function setSetupToken(token: string) {
  window.sessionStorage.setItem("aegiscore_setup_token", token.trim());
}
