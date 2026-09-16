"use client";

import { ErrorNotice, Empty, RuledLoading } from "@/components/register";

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
  label?: string;
  onRetry?: () => void;
}

export function PaginatedTable<T>({
  items, total, page, pageSize, onPageChange, onPageSizeChange, renderRow, renderHeader,
  loading, error, emptyMessage = "No records on file.", label, onRetry,
}: PaginatedTableProps<T>) {
  const totalItems = total ?? items.length;
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
  const start = (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalItems);

  if (error) return <ErrorNotice message={error} hint="Check that the university API is reachable, then try again." onRetry={onRetry} />;
  if (loading && items.length === 0) return <RuledLoading head={false} rows={Math.min(pageSize, 8)} label="Loading records" />;
  if (!loading && items.length === 0) return <Empty title={emptyMessage} />;

  return (
    <div className="ledger-wrap" aria-busy={loading || undefined}>
      <table className="ledger" aria-label={label}>
        {renderHeader && <thead>{renderHeader()}</thead>}
        <tbody>{items.map((item, i) => renderRow(item, i))}</tbody>
      </table>
      {(totalPages > 1 || onPageSizeChange) && (
        <nav className="ledger-foot" aria-label="Pagination">
          <span>Showing {start}–{end} of {totalItems}</span>
          <span className="btn-row">
            {onPageSizeChange && (
              <label style={{ display: "inline-flex", gap: 8, alignItems: "center" }}>Rows
                <select value={pageSize} onChange={e => onPageSizeChange(Number(e.target.value))} style={{ minHeight: 32, width: "auto" }}>
                  {[10, 20, 50, 100].map(n => <option key={n} value={n}>{n}</option>)}
                </select>
              </label>
            )}
            <button type="button" className="btn btn-secondary btn-sm" onClick={() => onPageChange(page - 1)} disabled={page <= 1}>Previous page</button>
            <span className="mono">Page {page} of {totalPages}</span>
            <button type="button" className="btn btn-secondary btn-sm" onClick={() => onPageChange(page + 1)} disabled={page >= totalPages}>Next page</button>
          </span>
        </nav>
      )}
    </div>
  );
}
