"use client";

import { useCallback } from "react";

export interface PaginatedTableProps<T> {
  items: T[];
  total?: number;
  page: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
  renderRow: (item: T, index: number) => React.ReactNode;
  renderHeader?: () => React.ReactNode;
  loading?: boolean;
  error?: string | null;
  emptyMessage?: string;
}

export function PaginatedTable<T>({
  items,
  total,
  page,
  pageSize,
  onPageChange,
  onPageSizeChange,
  renderRow,
  renderHeader,
  loading,
  error,
  emptyMessage = "No records found.",
}: PaginatedTableProps<T>) {
  const totalItems = total ?? items.length;
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
  const start = (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalItems);

  if (error) {
    return (
      <div className="p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">
        {error}
      </div>
    );
  }

  if (!loading && items.length === 0) {
    return (
      <div className="p-8 text-center text-slate-500 text-sm">{emptyMessage}</div>
    );
  }

  return (
    <div className="w-full">
      {loading && (
        <div className="absolute inset-0 bg-white/50 flex items-center justify-center z-10 rounded-lg">
          <div className="animate-spin h-6 w-6 border-2 border-indigo-600 border-t-transparent rounded-full" />
        </div>
      )}

      <div className="overflow-x-auto relative">
        <table className="w-full text-sm">
          {renderHeader && <thead>{renderHeader()}</thead>}
          <tbody>
            {items.map((item, i) => renderRow(item, i))}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between px-4 py-3 border-t border-slate-200 mt-2">
          <span className="text-xs text-slate-500">
            Showing {start}–{end} of {totalItems}
          </span>
          <div className="flex items-center gap-1">
            <button
              onClick={() => onPageChange(page - 1)}
              disabled={page <= 1}
              className="px-2 py-1 text-xs rounded border border-slate-300 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-slate-100"
            >
              Prev
            </button>
            {Array.from({ length: Math.min(totalPages, 7) }, (_, i) => {
              const p = i + Math.max(1, page - 3);
              if (p > totalPages) return null;
              return (
                <button
                  key={p}
                  onClick={() => onPageChange(p)}
                  className={`px-2 py-1 text-xs rounded border ${
                    p === page
                      ? "bg-indigo-600 text-white border-indigo-600"
                      : "border-slate-300 hover:bg-slate-100"
                  }`}
                >
                  {p}
                </button>
              );
            })}
            <button
              onClick={() => onPageChange(page + 1)}
              disabled={page >= totalPages}
              className="px-2 py-1 text-xs rounded border border-slate-300 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-slate-100"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
