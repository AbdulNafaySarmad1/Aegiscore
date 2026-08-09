"use client";

// ─── useAsyncData ───
// Standard data-fetching hook: loading / error / retry semantics in one place.
// 401s are handled transparently by the api-client (refresh + session expiry);
// this hook only surfaces terminal failures.

import { useCallback, useEffect, useRef, useState } from "react";

export type AsyncState<T> = {
  data: T | null;
  loading: boolean;
  error: unknown;
  reload: () => void;
};

export function useAsyncData<T>(fetcher: (signal: AbortSignal) => Promise<T>, deps: readonly unknown[] = []): AsyncState<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [attempt, setAttempt] = useState(0);
  const fetcherRef = useRef(fetcher);

  useEffect(() => { fetcherRef.current = fetcher; });

  useEffect(() => {
    const controller = new AbortController();
    fetcherRef.current(controller.signal)
      .then(result => { if (!controller.signal.aborted) { setData(result); setError(null); } })
      .catch(err => { if (!controller.signal.aborted) setError(err); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, attempt]);

  const reload = useCallback(() => {
    setLoading(true);
    setError(null);
    setAttempt(a => a + 1);
  }, []);
  return { data, loading, error, reload };
}
