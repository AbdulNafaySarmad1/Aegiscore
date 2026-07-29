import { FormEvent, useEffect, useState } from "react";
import { apiGet, apiPost } from "./api";

type Ticket = {
  id: string; title: string; description: string; category: string; priority: string;
  status: string; aiResponse?: string; aiConfidence?: number; customerRating?: number;
  customerFeedback?: string; humanResponse?: string; createdAt: string; resolvedAt?: string;
};
type Escalation = { id: string; reason: string; createdAt: string; ticket: Ticket };
type Analytics = { totalTickets: number; averageCsat: number; aiResolutionRate: number; escalationRate: number; averageResolutionHours: number };

export function Ticketing({ staff }: { staff: boolean }) {
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [selected, setSelected] = useState<Ticket | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [escalations, setEscalations] = useState<Escalation[]>([]);
  const [analytics, setAnalytics] = useState<Analytics | null>(null);

  async function refresh() {
    try {
      const list = await apiGet<Ticket[]>("/api/support/tickets");
      setTickets(list);
      if (selected) setSelected(list.find(x => x.id === selected.id) ?? selected);
      if (staff) {
        const [queue, metrics] = await Promise.all([
          apiGet<Escalation[]>("/api/support/escalations"),
          apiGet<Analytics>("/api/support/analytics")
        ]);
        setEscalations(queue);
        setAnalytics(metrics);
      }
      setError("");
    } catch (e) { setError(e instanceof Error ? e.message : "Support service is unavailable."); }
  }

  useEffect(() => { void refresh(); }, [staff]);

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true);
    try {
      const ticket = await apiPost<Ticket>("/api/support/tickets", {
        category: form.get("category"), title: form.get("title"),
        description: form.get("description"), priority: form.get("priority")
      });
      event.currentTarget.reset();
      setSelected(ticket);
      await refresh();
    } catch (e) { setError(e instanceof Error ? e.message : "Ticket could not be created."); }
    finally { setBusy(false); }
  }

  async function rate(ticket: Ticket, rating: number) {
    setBusy(true);
    try {
      const updated = await apiPost<Ticket>(`/api/support/tickets/${ticket.id}/feedback`, { rating, feedback: rating <= 2 ? "AI response was not helpful" : "Resolved" });
      setSelected(updated); await refresh();
    } catch (e) { setError(e instanceof Error ? e.message : "Feedback could not be saved."); }
    finally { setBusy(false); }
  }

  async function resolve(escalation: Escalation, response: string) {
    if (response.trim().length < 10) { setError("Enter a complete human response."); return; }
    setBusy(true);
    try {
      await apiPost(`/api/support/escalations/${escalation.id}/resolve`, { response, addToKnowledgeBase: true });
      await refresh();
    } catch (e) { setError(e instanceof Error ? e.message : "Escalation could not be resolved."); }
    finally { setBusy(false); }
  }

  if (staff) return <div className="support-admin">
    {error && <p className="support-error">{error}</p>}
    <div className="support-metrics">
      <Metric label="Open queue" value={String(escalations.length)} />
      <Metric label="AI resolution" value={`${analytics?.aiResolutionRate ?? 0}%`} />
      <Metric label="CSAT" value={`${analytics?.averageCsat ?? 0}/100`} />
      <Metric label="Avg resolution" value={`${analytics?.averageResolutionHours ?? 0} hrs`} />
    </div>
    <h3>Escalation queue</h3>
    {escalations.length === 0 && <p className="support-empty">All tickets are handled. No pending escalations.</p>}
    {escalations.map(item => <EscalationCard key={item.id} item={item} busy={busy} onResolve={resolve} />)}
  </div>;

  return <div className="support-layout">
    <form className="support-form" onSubmit={create}>
      <h3>Create a support ticket</h3>
      <label>Category<select name="category" defaultValue="other">
        <option value="password_reset">Forgot password</option><option value="grades">Grades</option>
        <option value="fees">Fees or payment</option><option value="attendance">Attendance</option><option value="other">Other</option>
      </select></label>
      <label>Priority<select name="priority" defaultValue="normal">
        <option value="low">Low</option><option value="normal">Normal</option><option value="high">High</option><option value="critical">Critical</option>
      </select></label>
      <label>Title<input name="title" minLength={4} maxLength={255} required placeholder="Briefly describe the issue" /></label>
      <label>What happened?<textarea name="description" minLength={10} maxLength={10000} required placeholder="Include the page, action, and any error message." /></label>
      <button disabled={busy} type="submit">{busy ? "Sending…" : "Send to AI support"}</button>
      {error && <p className="support-error">{error}</p>}
    </form>
    <section className="ticket-list">
      <h3>My tickets</h3>
      {tickets.length === 0 && <p className="support-empty">No tickets yet.</p>}
      {tickets.map(ticket => <button key={ticket.id} className={selected?.id === ticket.id ? "ticket active" : "ticket"} onClick={() => setSelected(ticket)}>
        <strong>{ticket.title}</strong><span>{ticket.status.replaceAll("_", " ")}</span><small>{new Date(ticket.createdAt).toLocaleString()}</small>
      </button>)}
    </section>
    <section className="ticket-detail">
      {!selected && <p className="support-empty">Select a ticket to view the response.</p>}
      {selected && <><div className="ticket-heading"><div><h3>{selected.title}</h3><span className={`ticket-status ${selected.status}`}>{selected.status.replaceAll("_", " ")}</span></div><small>{selected.category}</small></div>
        <p>{selected.description}</p>
        {selected.aiResponse && <article className="assistant-message"><b>AI assistant</b><p>{selected.aiResponse}</p>{selected.aiConfidence !== undefined && <small>Confidence: {Math.round(selected.aiConfidence * 100)}%</small>}</article>}
        {selected.humanResponse && <article className="human-message"><b>Support team</b><p>{selected.humanResponse}</p></article>}
        {selected.status === "open" && !selected.customerRating && <div className="feedback"><span>Was this helpful?</span><button disabled={busy} onClick={() => rate(selected, 5)}>Yes, resolved</button><button disabled={busy} onClick={() => rate(selected, 1)}>No, escalate</button></div>}
        {selected.status === "escalated" && <p className="support-note">This ticket is in the human support queue. Critical actions are never performed automatically.</p>}
      </>}
    </section>
  </div>;
}

function Metric({ label, value }: { label: string; value: string }) {
  return <article><strong>{value}</strong><span>{label}</span></article>;
}

function EscalationCard({ item, busy, onResolve }: { item: Escalation; busy: boolean; onResolve: (item: Escalation, response: string) => void }) {
  const [response, setResponse] = useState("");
  return <article className="escalation-card">
    <div><strong>{item.ticket.title}</strong><span>{item.ticket.priority}</span></div>
    <p>{item.ticket.description}</p><small>Reason: {item.reason}</small>
    {item.ticket.aiResponse && <blockquote>{item.ticket.aiResponse}</blockquote>}
    <textarea value={response} onChange={e => setResponse(e.target.value)} placeholder="Verified human response…" />
    <button disabled={busy} onClick={() => onResolve(item, response)}>Resolve and add to knowledge base</button>
  </article>;
}

