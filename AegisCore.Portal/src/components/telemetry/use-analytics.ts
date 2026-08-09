"use client";

// ─── Analytics Fetch Hook ───
// Small shared fetch state machine for analytics surfaces. Uses the shared
// api-client (cookie auth, transparent refresh, distinct 401/403).

import { useEffect, useState } from "react";

export type AnalyticsFetchState<T> =
  | { status: "loading" }
  | { status: "error"; error: unknown }
  | { status: "ok"; data: T };

type KeyedState<T> = { key: string; value: AnalyticsFetchState<T> };

/**
 * Fetches once per key change. `key` should be a stable primitive (or null
 * to skip fetching, e.g. while the session is still loading). State resets
 * via the render-adjust pattern; the effect only performs the fetch.
 */
export function useAnalyticsFetch<T>(
  key: string | null,
  fetcher: (signal: AbortSignal) => Promise<T>
): AnalyticsFetchState<T> {
  const [state, setState] = useState<KeyedState<T> | null>(null);

  // Render-adjust: a changed key resets to loading synchronously during
  // render (no cascading effect-driven state writes).
  if (key !== null && state?.key !== key) {
    setState({ key, value: { status: "loading" } });
  }

  useEffect(() => {
    if (key === null) return;
    const ctrl = new AbortController();
    fetcher(ctrl.signal)
      .then(data => { if (!ctrl.signal.aborted) setState({ key, value: { status: "ok", data } }); })
      .catch(error => { if (!ctrl.signal.aborted) setState({ key, value: { status: "error", error } }); });
    return () => ctrl.abort();
    // fetcher is stable per page render; key captures all routing inputs.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key]);

  if (key === null) return { status: "loading" };
  return state?.key === key ? state.value : { status: "loading" };
}
