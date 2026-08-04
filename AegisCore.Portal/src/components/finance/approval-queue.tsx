"use client";

import { apiPost } from "@/lib/api-client";
import { Check, X } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";

type Voucher = { id: string; voucherNumber: string; voucherType: string; currency: string; totalAmount: number; createdAt: string };

export function ApprovalQueue({ vouchers, locale, labels }: { vouchers: Voucher[]; locale: string; labels: { title: string; detail: string; approve: string; reject: string; note: string; required: string; failed: string } }) {
  const router = useRouter();
  const [notes, setNotes] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState<string | null>(null);
  const [message, setMessage] = useState("");
  const money = (value: number, currency: string) => new Intl.NumberFormat(locale, { style: "currency", currency, maximumFractionDigits: 0 }).format(value);

  async function decide(voucher: Voucher, approve: boolean) {
    const note = notes[voucher.id]?.trim() ?? "";
    if (!approve && !note) { setMessage(labels.required); return; }
    setBusy(voucher.id); setMessage("");
    try {
      await apiPost(`/api/finance/v2/vouchers/${voucher.id}/decision`, { approve, note: note || null });
      router.refresh();
    } catch { setMessage(labels.failed); }
    finally { setBusy(null); }
  }

  return <section className="surface finance-panel"><div className="surface-heading"><div><h2>{labels.title}</h2><p>{labels.detail}</p></div><strong>{vouchers.length}</strong></div>
    <p className="sr-only" aria-live="polite">{message}</p>
    <ol className="approval-list">{vouchers.map(voucher => <li key={voucher.id}>
      <span><strong>{voucher.voucherNumber}</strong><small>{voucher.voucherType} · {money(voucher.totalAmount, voucher.currency)}</small></span>
      <label><span className="sr-only">{labels.note} {voucher.voucherNumber}</span><input value={notes[voucher.id] ?? ""} onChange={event => setNotes(current => ({ ...current, [voucher.id]: event.target.value }))} placeholder={labels.note} maxLength={500} /></label>
      <button className="icon-action approve" disabled={busy === voucher.id} onClick={() => decide(voucher, true)} title={labels.approve} aria-label={`${labels.approve} ${voucher.voucherNumber}`}><Check /></button>
      <button className="icon-action reject" disabled={busy === voucher.id} onClick={() => decide(voucher, false)} title={labels.reject} aria-label={`${labels.reject} ${voucher.voucherNumber}`}><X /></button>
    </li>)}</ol>
    {message && <p className="finance-alert" role="alert">{message}</p>}
  </section>;
}
