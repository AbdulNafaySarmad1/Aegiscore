"use client";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";

export function RecordTable({ title, endpoint, columns }: { title: string; endpoint: string; columns: { key: string; label: string }[] }) {
  const [rows, setRows] = useState<Record<string, unknown>[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [retry, setRetry] = useState(0);
  useEffect(() => {
    const abort = new AbortController();
    apiGet<Record<string, unknown>[] | { items: Record<string, unknown>[]; total: number }>(`${endpoint}?page=${page}`, abort.signal)
      .then(data => { setRows(Array.isArray(data) ? data : data.items); setTotal(Array.isArray(data) ? data.length : data.total); setError(""); })
      .catch(reason => { if (!abort.signal.aborted) setError(reason.message); })
      .finally(() => { if (!abort.signal.aborted) setLoading(false); });
    return () => abort.abort();
  }, [endpoint, page, retry]);
  return <AppShell><div className="content-page"><header className="page-header"><h1>{title}</h1></header>
    {error ? <div role="alert"><p>{error}</p><button onClick={() => setRetry(value => value + 1)}>Retry</button></div> : loading ? <p>Loading {title.toLowerCase()}…</p> : <>
      <p>{total} records</p><div className="card" style={{ overflowX: "auto" }}><table className="data-table"><thead><tr>{columns.map(column => <th scope="col" key={column.key}>{column.label}</th>)}</tr></thead>
        <tbody>{rows.map((row, index) => <tr key={String(row.id ?? index)}>{columns.map(column => <td key={column.key}>{String(row[column.key] ?? "—")}</td>)}</tr>)}</tbody></table>
        {!rows.length && <p>No records are available.</p>}</div>
      {endpoint === "/api/students" && <nav aria-label="Student pages"><button disabled={page === 1} onClick={() => setPage(page - 1)}>Previous</button><span> Page {page} </span><button disabled={page * 50 >= total} onClick={() => setPage(page + 1)}>Next</button></nav>}
    </>}
  </div></AppShell>;
}
