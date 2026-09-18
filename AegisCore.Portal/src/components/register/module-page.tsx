"use client";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { ApiError } from "@/lib/api-client";
import { RegisterHead, ErrorNotice, RuledLoading, Notice, type Crumb, type Tone } from "@/components/register";

export type ModuleState<T> = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; data: T };

/**
 * The shape every module screen shares: a running head, a ruled skeleton while the registers are
 * read, one error notice with a retry when they cannot be, and a reload the write actions call.
 * Written once so twenty screens do not each invent a loading spinner and an error box.
 */
export function useModuleData<T>(read: (signal: AbortSignal) => Promise<T>, key = "") {
  const [state, setState] = useState<ModuleState<T>>({ kind: "loading" });
  const [reloads, setReloads] = useState(0);
  // The read closure is rebuilt on every render; `key` is what actually changes it (a filter, a
  // selected scope), so the effect re-runs on that and on an explicit reload, not on every render.
  const latest = useRef(read);
  useEffect(() => { latest.current = read; });

  useEffect(() => {
    const abort = new AbortController();
    latest.current(abort.signal)
      .then(data => { if (!abort.signal.aborted) setState({ kind: "ready", data }); })
      .catch(error => { if (!abort.signal.aborted) setState({ kind: "error", message: error instanceof ApiError ? error.message : "These records could not be read." }); });
    return () => abort.abort();
  }, [key, reloads]);

  return { state, reload: () => setReloads(n => n + 1) };
}

export function ModulePage<T>({ title, crumbs, ident, meta, state, reload, children, notice }: {
  title: ReactNode;
  crumbs: Crumb[];
  ident?: (data: T) => ReactNode;
  meta?: ReactNode;
  state: ModuleState<T>;
  reload: () => void;
  notice?: { tone: Tone; text: string } | null;
  children: (data: T) => ReactNode;
}) {
  if (state.kind === "loading") return <div className="page"><RuledLoading label={`Opening the ${String(title).toLowerCase()} register`} rows={6} /></div>;
  if (state.kind === "error") return (
    <div className="page">
      <RegisterHead title={title} crumbs={crumbs} reveal={false} />
      <ErrorNotice message={state.message} hint="Check your connection and try again." onRetry={reload} />
    </div>
  );
  return (
    <div className="page">
      <RegisterHead title={title} crumbs={crumbs} ident={ident?.(state.data)} meta={meta} />
      {notice && <Notice tone={notice.tone} role="status">{notice.text}</Notice>}
      {children(state.data)}
    </div>
  );
}

/** A form inside a panel, with its own busy state and outcome line. */
export function useAction() {
  const [busy, setBusy] = useState<string | null>(null);
  const [outcome, setOutcome] = useState<{ tone: Tone; text: string } | null>(null);
  async function run(name: string, work: () => Promise<string>, after?: () => void) {
    setBusy(name); setOutcome(null);
    try { setOutcome({ tone: "laurel", text: await work() }); after?.(); }
    catch (error) { setOutcome({ tone: "oxblood", text: error instanceof ApiError ? error.message : "That did not complete." }); }
    finally { setBusy(null); }
  }
  return { busy, outcome, run, clear: () => setOutcome(null) };
}
