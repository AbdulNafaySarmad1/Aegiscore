export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly payload?: unknown
  ) {
    super(message);
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: "include",
    ...init,
    headers: init?.body instanceof FormData
      ? init.headers
      : { "Content-Type": "application/json", ...init?.headers }
  });
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
