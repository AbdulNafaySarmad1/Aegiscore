"use client";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";
import { RegisterHead, RuledLoading, ErrorNotice, Empty } from "@/components/register";

const PAGE_SIZE = 50;

export function RecordTable({ title, endpoint, columns, description }: {
  title: string; endpoint: string; description?: string;
  columns: { key: string; label: string; mono?: boolean; numeric?: boolean }[];
}) {
  const [rows, setRows] = useState<Record<string, unknown>[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [retry, setRetry] = useState(0);

  useEffect(() => {
    const abort = new AbortController();
    // eslint-disable-next-line react-hooks/set-state-in-effect -- show the ruled loader while a page loads.
    setRows(null); setError(null);
    apiGet<Record<string, unknown>[] | { items: Record<string, unknown>[]; total: number }>(`${endpoint}?page=${page}`, abort.signal)
      .then(data => { setRows(Array.isArray(data) ? data : data.items); setTotal(Array.isArray(data) ? data.length : data.total); })
      .catch(reason => { if (!abort.signal.aborted) { setRows([]); setError(reason instanceof Error ? reason.message : "The records could not be loaded."); } });
    return () => abort.abort();
  }, [endpoint, page, retry]);

  const paged = endpoint === "/api/students";
  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));
  const first = (page - 1) * PAGE_SIZE + 1;
  const last = Math.min(page * PAGE_SIZE, total);

  return (
    <AppShell>
      <div className="page">
        <RegisterHead
          title={title}
          ident={rows ? <strong>{total} record{total === 1 ? "" : "s"}</strong> : "Counting…"}
          meta={description ? <span>{description}</span> : undefined}
        />
        {rows === null ? (
          <RuledLoading label={`Loading ${title.toLowerCase()}`} rows={8} head={false} />
        ) : error ? (
          <ErrorNotice message={error} hint="Check that the university API is reachable, then try again." onRetry={() => setRetry(n => n + 1)} />
        ) : rows.length === 0 ? (
          <div className="panel"><Empty title={`No ${title.toLowerCase()} on record.`}>Records appear here as soon as the office enters them.</Empty></div>
        ) : (
          <div className="ledger-wrap">
            <table className="ledger" aria-label={title}>
              <thead><tr>{columns.map(c => <th scope="col" key={c.key} className={c.numeric ? "num" : undefined}>{c.label}</th>)}</tr></thead>
              <tbody>
                {rows.map((row, index) => (
                  <tr key={String(row.id ?? index)}>
                    {columns.map(c => <td key={c.key} className={[c.mono ? "mono" : "", c.numeric ? "num" : ""].join(" ").trim() || undefined}>{row[c.key] == null || row[c.key] === "" ? <span className="text-muted">—</span> : String(row[c.key])}</td>)}
                  </tr>
                ))}
              </tbody>
            </table>
            {paged && (
              <nav className="ledger-foot" aria-label={`${title} pages`}>
                <span>Showing {first}–{last} of {total}</span>
                <span className="btn-row">
                  <button type="button" className="btn btn-secondary btn-sm" disabled={page === 1} onClick={() => setPage(page - 1)}>Previous page</button>
                  <span className="mono">Page {page} of {pages}</span>
                  <button type="button" className="btn btn-secondary btn-sm" disabled={page >= pages} onClick={() => setPage(page + 1)}>Next page</button>
                </span>
              </nav>
            )}
          </div>
        )}
      </div>
    </AppShell>
  );
}
