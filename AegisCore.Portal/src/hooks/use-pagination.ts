"use client";

import { useState, useCallback } from "react";
import { apiGet } from "@/lib/api-client";

export interface PaginatedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export function usePaginatedFetch<T>(basePath: string, defaultPageSize = 20) {
  const [data, setData] = useState<PaginatedResult<T>>({
    items: [],
    total: 0,
    page: 1,
    pageSize: defaultPageSize,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetch = useCallback(
    async (page?: number, pageSize?: number) => {
      const p = page ?? data.page;
      const ps = pageSize ?? data.pageSize;
      setLoading(true);
      setError(null);
      try {
        const separator = basePath.includes("?") ? "&" : "?";
        const result = await apiGet<PaginatedResult<T>>(
          `${basePath}${separator}page=${p}&pageSize=${ps}`
        );
        setData({
          items: result.items ?? result.items ?? [],
          total: result.total ?? 0,
          page: p,
          pageSize: ps,
        });
      } catch (e) {
        setError(e instanceof Error ? e.message : "The requested page of data could not be loaded.");
      } finally {
        setLoading(false);
      }
    },
    [basePath, data.page, data.pageSize]
  );

  const nextPage = useCallback(() => {
    if (data.page * data.pageSize < data.total) fetch(data.page + 1);
  }, [data, fetch]);

  const prevPage = useCallback(() => {
    if (data.page > 1) fetch(data.page - 1);
  }, [data, fetch]);

  const setPageSize = useCallback(
    (size: number) => fetch(1, size),
    [fetch]
  );

  return { data, loading, error, fetch, nextPage, prevPage, setPageSize };
}

export function useLocalPagination<T>(items: T[], defaultPageSize = 20) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(defaultPageSize);

  const totalPages = Math.ceil(items.length / pageSize);
  const paginatedItems = items.slice((page - 1) * pageSize, page * pageSize);

  return {
    items: paginatedItems,
    total: items.length,
    page,
    pageSize,
    totalPages,
    setPage,
    setPageSize: (size: number) => {
      setPageSize(size);
      setPage(1);
    },
    nextPage: () => setPage((p) => Math.min(p + 1, totalPages)),
    prevPage: () => setPage((p) => Math.max(p - 1, 1)),
  };
}
