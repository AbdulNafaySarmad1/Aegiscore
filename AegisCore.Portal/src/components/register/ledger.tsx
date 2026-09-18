"use client";
import type { ReactNode } from "react";
import { Empty, type Tone } from "@/components/register";

/** One column of a ledger: what to show in the head and how to render a row's cell. */
export type Column<T> = {
  key: string;
  label: ReactNode;
  render: (row: T) => ReactNode;
  /** Right-aligned tabular figures. */
  num?: boolean;
  /** Identifier set in mono. */
  mono?: boolean;
  nowrap?: boolean;
};

/**
 * A ruled table in the register vocabulary, with the empty state built in so a module page never
 * renders an icon in a grey box. `status` colours the left rule of a row (laurel/oxblood/brass).
 */
export function Ledger<T>({ columns, rows, keyOf, empty, status, caption, minWidth }: {
  columns: Column<T>[];
  rows: T[];
  keyOf: (row: T) => string;
  empty: { title: ReactNode; detail?: ReactNode; action?: ReactNode };
  status?: (row: T) => Tone | undefined;
  caption?: string;
  minWidth?: number;
}) {
  if (rows.length === 0) return <Empty title={empty.title} action={empty.action}>{empty.detail}</Empty>;
  return (
    <div className="ledger-wrap">
      <table className="ledger" style={minWidth ? { minWidth } : undefined}>
        {caption && <caption className="sr-only">{caption}</caption>}
        <thead>
          <tr>{columns.map(c => <th key={c.key} className={c.num ? "num" : undefined} scope="col">{c.label}</th>)}</tr>
        </thead>
        <tbody>
          {rows.map(row => (
            <tr key={keyOf(row)} data-status={status?.(row)}>
              {columns.map(c => <td key={c.key} className={[c.num ? "num" : "", c.mono ? "mono" : "", c.nowrap ? "nowrap" : ""].filter(Boolean).join(" ") || undefined}>{c.render(row)}</td>)}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

/** A yes/no or state word as a typographic status mark, from any status vocabulary the API uses. */
export function toneOf(value: string | boolean | null | undefined): Tone {
  if (typeof value === "boolean") return value ? "laurel" : "oxblood";
  const v = String(value ?? "").toLowerCase();
  if (/(active|approved|cleared|paid|passed|completed|resolved|verified|valid|ok|healthy|success|settled|elected|certified|available|open$)/.test(v)) return "laurel";
  if (/(critical|overdue|failed|rejected|revoked|expired|blocked|breach|error|retired|outofservice|out_of_service|crisis|red|suspended|invalid)/.test(v)) return "oxblood";
  if (/(pending|warning|review|maintenance|partial|scheduled|awaiting|draft|nominated|urgent|yellow|orange|at_risk|in_progress|inprogress|processing)/.test(v)) return "brass";
  return "ink";
}

/** "OutOfService" → "Out of service"; "at_risk" → "At risk". Status words come from enums; people read sentences. */
export function words(value: string | null | undefined): string {
  if (!value) return "—";
  const spaced = value.replace(/[_-]+/g, " ").replace(/([a-z0-9])([A-Z])/g, "$1 $2").trim().toLowerCase();
  return spaced.charAt(0).toUpperCase() + spaced.slice(1);
}
