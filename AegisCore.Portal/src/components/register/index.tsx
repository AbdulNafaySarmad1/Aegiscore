"use client";

import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import Link from "next/link";
import { AlertTriangle, CircleAlert, Info, CircleCheck, RotateCw } from "lucide-react";
import { FocusTrapDialog } from "@/components/FocusTrapDialog";

export type Tone = "ink" | "brass" | "oxblood" | "laurel";

/* ── Running head ──────────────────────────────────────────── */
export type Crumb = { label: string; href?: string };

export function RegisterHead({ title, ident, crumbs, meta, actions, reveal = true }: {
  title: ReactNode;
  /** Record identifier shown in mono on the right (student number, application ID, count). */
  ident?: ReactNode;
  crumbs?: Crumb[];
  meta?: ReactNode;
  actions?: ReactNode;
  /** Draw the brass rule in on first mount (once per session). */
  reveal?: boolean;
}) {
  // Decided once per mount; the attribute is only ever added after hydration so the
  // server and client markup agree on first paint.
  const [drawn, setDrawn] = useState(false);
  useEffect(() => {
    if (!reveal) return;
    let first = true;
    try {
      first = !sessionStorage.getItem("aegis-rule-drawn");
      if (first) sessionStorage.setItem("aegis-rule-drawn", "1");
    } catch { /* storage unavailable: draw anyway */ }
    if (first) requestAnimationFrame(() => setDrawn(true));
  }, [reveal]);
  return (
    <header className="register-head" data-reveal={drawn ? "" : undefined}>
      {crumbs && crumbs.length > 0 && (
        <nav className="register-head-crumb" aria-label="Breadcrumb">
          {crumbs.map((c, i) => (
            <span key={`${c.label}-${i}`} style={{ display: "contents" }}>
              {i > 0 && <span aria-hidden="true">/</span>}
              {c.href ? <Link href={c.href}>{c.label}</Link> : <span aria-current="page">{c.label}</span>}
            </span>
          ))}
        </nav>
      )}
      <div className="register-head-title"><h1>{title}</h1></div>
      {ident !== undefined && <div className="register-head-ident">{ident}</div>}
      {meta !== undefined && <div className="register-head-meta">{meta}</div>}
      {actions !== undefined && <div className="register-head-actions">{actions}</div>}
      <div className="register-head-rule" aria-hidden="true" />
    </header>
  );
}

/* ── Panel ─────────────────────────────────────────────────── */
export function Panel({ title, aside, children, foot, flush, sealed, tone, className, id, as: Tag = "section" }: {
  title?: ReactNode; aside?: ReactNode; children?: ReactNode; foot?: ReactNode; flush?: boolean;
  sealed?: boolean; tone?: Tone; className?: string; id?: string; as?: "section" | "article" | "div";
}) {
  const headingId = useId();
  return (
    <Tag className={`panel${className ? ` ${className}` : ""}`} data-sealed={sealed ? "" : undefined} data-tone={tone} aria-labelledby={title ? headingId : undefined} id={id}>
      {(title || aside) && (
        <div className="panel-head">
          {title && <h2 className="panel-title" id={headingId}>{title}</h2>}
          {aside && <div className="panel-aside">{aside}</div>}
        </div>
      )}
      {children !== undefined && <div className={`panel-body${flush ? " flush" : ""}`}>{children}</div>}
      {foot && <div className="panel-foot">{foot}</div>}
    </Tag>
  );
}

/* ── Facts (dl) ────────────────────────────────────────────── */
export type Fact = { label: string; value: ReactNode; mono?: boolean };
export function Facts({ items, inline, "aria-label": ariaLabel }: { items: Fact[]; inline?: boolean; "aria-label"?: string }) {
  return (
    <dl className={inline ? "facts-inline" : "facts"} aria-label={ariaLabel}>
      {items.map(f => (
        <div key={f.label}>
          <dt>{f.label}</dt>
          <dd className={f.mono ? "mono" : undefined}>{f.value}</dd>
        </div>
      ))}
    </dl>
  );
}

/* ── Figures strip ─────────────────────────────────────────── */
export type Figure = { label: string; value: ReactNode; note?: ReactNode; tone?: Tone; mono?: boolean };
export function Figures({ items, "aria-label": ariaLabel }: { items: Figure[]; "aria-label"?: string }) {
  return (
    <dl className="figures" aria-label={ariaLabel}>
      {items.map(f => (
        <div className="figure" key={f.label} data-tone={f.tone}>
          <dt className="figure-label">{f.label}</dt>
          <dd className={`figure-value${f.mono ? " mono" : typeof f.value === "string" && /[a-z]{3,}/i.test(f.value) ? " text" : ""}`}>{f.value}</dd>
          {f.note && <dd className="figure-note">{f.note}</dd>}
        </div>
      ))}
    </dl>
  );
}

/* ── Status mark ───────────────────────────────────────────── */
export function Mark({ tone = "ink", children }: { tone?: Tone; children: ReactNode }) {
  return <span className="mark" data-tone={tone}>{children}</span>;
}

/* ── Notice ────────────────────────────────────────────────── */
export function Notice({ tone = "ink", title, children, actions, detail, role }: {
  tone?: Tone; title?: ReactNode; children?: ReactNode; actions?: ReactNode; detail?: string | null; role?: "alert" | "status";
}) {
  const Icon = tone === "oxblood" ? CircleAlert : tone === "brass" ? AlertTriangle : tone === "laurel" ? CircleCheck : Info;
  return (
    <div className="notice" data-tone={tone} role={role ?? (tone === "oxblood" ? "alert" : "status")}>
      <Icon aria-hidden="true" />
      <div className="notice-body">
        {title && <strong>{title}</strong>}
        {children && <p>{children}</p>}
      </div>
      {actions && <div className="notice-actions">{actions}</div>}
      {detail && <details><summary>Technical detail</summary><pre>{detail}</pre></details>}
    </div>
  );
}

/** Error line with a retry action; message is what happened, hint is what to do. */
export function ErrorNotice({ message, hint, onRetry, detail }: { message: string; hint?: string; onRetry?: () => void; detail?: string | null }) {
  return (
    <Notice tone="oxblood" title={message} detail={detail}
      actions={onRetry ? <button type="button" className="btn btn-secondary btn-sm" onClick={onRetry}><RotateCw aria-hidden="true" />Try again</button> : undefined}>
      {hint}
    </Notice>
  );
}

/* ── Empty state ───────────────────────────────────────────── */
export function Empty({ title, children, action }: { title: ReactNode; children?: ReactNode; action?: ReactNode }) {
  return (
    <div className="empty">
      <h3>{title}</h3>
      {children && <p>{children}</p>}
      {action}
    </div>
  );
}

/* ── Ruled loading skeleton ────────────────────────────────── */
export function RuledLoading({ rows = 6, head = true, label = "Loading" }: { rows?: number; head?: boolean; label?: string }) {
  return (
    <div className="ruled-loading" role="status" aria-live="polite" aria-label={label}>
      {head && <div className="ruled-loading-head" aria-hidden="true"><i /><i /><b /></div>}
      <div className="ruled-loading-rows" aria-hidden="true">
        {Array.from({ length: rows }, (_, i) => <i key={i}><span /><span /></i>)}
      </div>
      <p>{label}…</p>
    </div>
  );
}

/* ── Seal ──────────────────────────────────────────────────── */
export function Seal({ tone = "laurel", stamped, label = "University seal", size = 84 }: { tone?: Tone; stamped?: boolean; label?: string; size?: number }) {
  return (
    <span className="seal" data-tone={tone} data-stamped={stamped ? "" : undefined} role="img" aria-label={label} style={{ width: size, height: size }}>
      <svg viewBox="0 0 100 100" fill="none" stroke="currentColor" aria-hidden="true">
        <circle cx="50" cy="50" r="46" strokeWidth="1.5" />
        <circle cx="50" cy="50" r="41" strokeWidth=".75" />
        <circle cx="50" cy="50" r="30" strokeWidth="1" />
        <path d="M50 24 l6 12 13 2 -9.5 9.2 2.3 13.1L50 54.2 38.2 60.3l2.3-13.1L31 38l13-2z" strokeWidth="1.2" strokeLinejoin="round" />
        <path d="M32 72 q18 10 36 0" strokeWidth="1.2" strokeLinecap="round" />
        <path d="M22 50 h6 M72 50 h6" strokeWidth="1" />
      </svg>
    </span>
  );
}

/* ── Requirements list ─────────────────────────────────────── */
export function Requirements({ items }: { items: { label: ReactNode; state: "met" | "blocked" | "open"; note?: ReactNode; action?: ReactNode }[] }) {
  return (
    <ul className="requirements">
      {items.map((r, i) => (
        <li key={i} data-state={r.state}>
          <span>{r.label}{r.note && <small> {r.note}</small>}</span>
          {r.action ?? <span />}
        </li>
      ))}
    </ul>
  );
}

/* ── Progress rule ─────────────────────────────────────────── */
export function ProgressRule({ value, max, label, tone }: { value: number; max: number; label?: string; tone?: Tone }) {
  const pct = max > 0 ? Math.max(0, Math.min(100, (value / max) * 100)) : 0;
  return (
    <div className="progress-rule" data-tone={tone} role="progressbar" aria-valuenow={value} aria-valuemin={0} aria-valuemax={max} aria-label={label}>
      <div className="progress-rule-track"><div className="progress-rule-fill" style={{ width: `${pct}%` }} /></div>
      {label && <div className="progress-rule-label"><span>{label}</span><span className="mono">{value} of {max}</span></div>}
    </div>
  );
}

/* ── Prompt dialog (replaces window.prompt) ────────────────── */
export function PromptDialog({ title, description, label, placeholder, required, confirmLabel, tone = "ink", onConfirm, onCancel, busy }: {
  title: string; description?: ReactNode; label: string; placeholder?: string; required?: boolean;
  confirmLabel: string; tone?: Tone; onConfirm: (value: string) => void; onCancel: () => void; busy?: boolean;
}) {
  const [value, setValue] = useState("");
  const [touched, setTouched] = useState(false);
  const id = useId();
  const invalid = required && touched && value.trim().length === 0;
  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    setTouched(true);
    if (required && value.trim().length === 0) return;
    onConfirm(value.trim());
  };
  return (
    <div className="dialog-scrim">
      <FocusTrapDialog className="dialog" tone={tone} labelledBy={`${id}-title`} onClose={onCancel}>
          <h2 id={`${id}-title`}>{title}</h2>
          {description && <p>{description}</p>}
          <form onSubmit={submit} className="field" data-invalid={invalid ? "" : undefined}>
            <label htmlFor={`${id}-input`} className="field-label">{label}{required ? "" : " (optional)"}</label>
            <textarea id={`${id}-input`} value={value} placeholder={placeholder} onChange={e => setValue(e.target.value)} onBlur={() => setTouched(true)} aria-invalid={invalid || undefined} aria-describedby={invalid ? `${id}-err` : undefined} rows={3} />
            {invalid && <span className="field-error" id={`${id}-err`}>Enter a reason before continuing.</span>}
            <div className="dialog-actions">
              <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={busy}>Cancel</button>
              <button type="submit" className={`btn ${tone === "oxblood" ? "btn-danger" : "btn-primary"}${busy ? " btn-loading" : ""}`} disabled={busy}>{confirmLabel}</button>
            </div>
          </form>
      </FocusTrapDialog>
    </div>
  );
}

/* ── Live region helper for save confirmations ─────────────── */
export function useAnnounce() {
  const ref = useRef<HTMLDivElement | null>(null);
  const announce = (text: string) => { if (ref.current) { ref.current.textContent = ""; requestAnimationFrame(() => { if (ref.current) ref.current.textContent = text; }); } };
  const region = <div ref={ref} className="sr-only" role="status" aria-live="polite" />;
  return { announce, region };
}
