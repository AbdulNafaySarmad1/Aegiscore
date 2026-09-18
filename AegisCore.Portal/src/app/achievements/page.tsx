"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import { apiGet, apiPost, ApiError } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { RegisterHead, Panel, Figures, Mark, Notice, ErrorNotice, RuledLoading } from "@/components/register";
import { Ledger, toneOf } from "@/components/register/ledger";

type Badge = { id: string; name: string; category: string; tier: "Bronze" | "Silver" | "Gold" | "Platinum"; criteria: string; imageUrl: string };
type Achievement = { studentId: string; studentName: string; badgeId: string; badgeName: string; earnedAt: string; triggerEvent: string };
type DeansEntry = { studentId: string; studentName: string; gpa: number; semester: string; entryType: "DeansList" | "HonorsRoll"; processedAt: string };
type LeaderboardEntry = { rank: number; studentId: string; studentName: string; totalScore: number; badgeCount: number; topBadge: string };
type Data = { badges: Badge[]; achievements: Achievement[]; deans: DeansEntry[]; leaderboard: LeaderboardEntry[] };
type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; data: Data };

const scopes = [["all", "All students"], ["department", "Department"], ["batch", "Batch"], ["semester", "Current semester"]] as const;

/**
 * Achievements: the badge catalogue, what students have earned, the dean's list and the
 * leaderboard — all read from the register; nothing here is estimated.
 */
export default function AchievementsPage() {
  const { date } = useTenantFormatter();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [retry, setRetry] = useState(0);
  const [scope, setScope] = useState<string>("all");
  const [detectStudentId, setDetectStudentId] = useState("");
  const [deansSemester, setDeansSemester] = useState("");
  const [busy, setBusy] = useState<"detect" | "deans" | null>(null);
  const [note, setNote] = useState<{ tone: "laurel" | "oxblood" | "ink"; text: string } | null>(null);

  const load = useCallback(async (signal?: AbortSignal): Promise<State> => {
    try {
      const [badges, achievements, deans, leaderboard] = await Promise.all([
        apiGet<Badge[]>("/api/achievements/badges", signal),
        apiGet<Achievement[]>("/api/achievements/recent", signal),
        apiGet<DeansEntry[]>("/api/achievements/deans-list", signal),
        apiGet<LeaderboardEntry[]>(`/api/achievements/leaderboard?scope=${scope}`, signal),
      ]);
      return { kind: "ready", data: { badges, achievements, deans, leaderboard } };
    } catch (error) {
      return { kind: "error", message: error instanceof ApiError ? error.message : "Achievement records could not be read." };
    }
  }, [scope]);

  useEffect(() => {
    const abort = new AbortController();
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reset on retry.
    setState(s => (s.kind === "ready" ? s : { kind: "loading" }));
    load(abort.signal).then(next => { if (!abort.signal.aborted) setState(next); });
    return () => abort.abort();
  }, [load, retry]);

  async function detect(e: FormEvent) {
    e.preventDefault();
    if (!detectStudentId.trim()) return;
    setBusy("detect"); setNote(null);
    try {
      const res = await apiPost<{ newBadges: string[] }>("/api/achievements/detect", { studentId: detectStudentId.trim() });
      setNote(res.newBadges.length > 0 ? { tone: "laurel", text: `${res.newBadges.length} new badge(s) awarded: ${res.newBadges.join(", ")}.` } : { tone: "ink", text: "No new badge is due for this student." });
      setRetry(n => n + 1);
    } catch (error) { setNote({ tone: "oxblood", text: error instanceof ApiError ? error.message : "Detection did not complete." }); }
    finally { setBusy(null); }
  }

  async function processDeans(e: FormEvent) {
    e.preventDefault();
    if (!deansSemester.trim()) return;
    setBusy("deans"); setNote(null);
    try {
      await apiPost("/api/achievements/deans-list/process", { semester: deansSemester.trim() });
      setNote({ tone: "laurel", text: `Dean's list processed for ${deansSemester.trim()}.` });
      setDeansSemester("");
      setRetry(n => n + 1);
    } catch (error) { setNote({ tone: "oxblood", text: error instanceof ApiError ? error.message : "Processing did not complete." }); }
    finally { setBusy(null); }
  }

  const crumbs = [{ label: "Student welfare" }, { label: "Achievements" }];
  if (state.kind === "loading") return <div className="page"><RuledLoading label="Opening the achievements register" rows={6} /></div>;
  if (state.kind === "error") return (
    <div className="page">
      <RegisterHead title="Achievements" crumbs={crumbs} reveal={false} />
      <ErrorNotice message={state.message} hint="Check your connection and try again." onRetry={() => setRetry(n => n + 1)} />
    </div>
  );

  const { badges, achievements, deans, leaderboard } = state.data;
  const top = leaderboard[0];
  return (
    <div className="page">
      <RegisterHead
        title="Achievements"
        crumbs={crumbs}
        ident={<><strong>{achievements.length}</strong><br />badges earned</>}
        meta={<span>Badge catalogue, awards, the dean&apos;s list and the leaderboard, as recorded.</span>}
      />

      <Figures aria-label="Achievement figures" items={[
        { label: "Badges in the catalogue", value: badges.length, note: `${new Set(badges.map(b => b.category)).size} categories` },
        { label: "Badges earned", value: achievements.length, note: "across all students" },
        { label: "On the dean's list", value: deans.length, note: deans.length === 0 ? "no semester processed yet" : "honoured students" },
        { label: "Top of the leaderboard", value: top ? top.studentName : "—", note: top ? `${top.totalScore} points, ${top.badgeCount} badges` : "no ranked students" },
      ]} />

      {note && <Notice tone={note.tone} role="status">{note.text}</Notice>}

      <div className="record">
        <div className="record-body">
          <Panel title="Badge catalogue" aside={<span className="text-muted">{badges.length} available</span>} flush>
            <Ledger rows={badges} keyOf={b => b.id} minWidth={560}
              empty={{ title: "No badges configured", detail: "Badges are defined by the student-affairs office; none has been created for this institution yet." }}
              columns={[
                { key: "name", label: "Badge", render: b => <strong>{b.name}</strong> },
                { key: "category", label: "Category", render: b => b.category },
                { key: "tier", label: "Tier", render: b => <Mark tone={b.tier === "Platinum" || b.tier === "Gold" ? "brass" : "ink"}>{b.tier}</Mark> },
                { key: "criteria", label: "Criteria", render: b => <span className="text-muted">{b.criteria}</span> },
              ]} />
          </Panel>

          <Panel title="Leaderboard" flush aside={
            <label className="toolbar-inline"><span className="sr-only">Scope</span>
              <select value={scope} onChange={e => setScope(e.target.value)}>{scopes.map(([v, l]) => <option key={v} value={v}>{l}</option>)}</select>
            </label>}>
            <Ledger rows={leaderboard} keyOf={r => r.studentId} minWidth={560}
              empty={{ title: "No ranked students", detail: "The leaderboard is built from recorded results; it fills as marks are entered." }}
              columns={[
                { key: "rank", label: "Rank", render: r => r.rank, num: true },
                { key: "student", label: "Student", render: r => r.studentName },
                { key: "id", label: "Student number", render: r => r.studentId, mono: true },
                { key: "score", label: "Score", render: r => r.totalScore, num: true },
                { key: "badges", label: "Badges", render: r => r.badgeCount, num: true },
                { key: "top", label: "Top badge", render: r => r.topBadge || "—" },
              ]} />
          </Panel>

          <Panel title="Dean's list and honours roll" aside={<span className="text-muted">{deans.length} entries</span>} flush>
            <Ledger rows={deans} keyOf={d => `${d.studentId}-${d.semester}`} minWidth={560}
              empty={{ title: "No dean's list entries", detail: "Process a semester to list the students at or above the honours threshold." }}
              status={d => toneOf(d.entryType === "DeansList" ? "approved" : "pending")}
              columns={[
                { key: "student", label: "Student", render: d => d.studentName },
                { key: "semester", label: "Semester", render: d => d.semester },
                { key: "gpa", label: "GPA", render: d => d.gpa.toFixed(2), num: true },
                { key: "type", label: "Listing", render: d => d.entryType === "DeansList" ? "Dean's list" : "Honours roll" },
                { key: "when", label: "Processed", render: d => date(d.processedAt), nowrap: true },
              ]} />
          </Panel>

          <Panel title="Recent awards" aside={<span className="text-muted">{achievements.length} earned</span>} flush>
            <Ledger rows={achievements} keyOf={a => `${a.studentId}-${a.badgeId}-${a.earnedAt}`} minWidth={560}
              empty={{ title: "No badges earned yet", detail: "Run detection for a student, or wait for the events that award badges to be recorded." }}
              columns={[
                { key: "student", label: "Student", render: a => a.studentName },
                { key: "badge", label: "Badge", render: a => a.badgeName },
                { key: "event", label: "Trigger", render: a => <span className="text-muted">{a.triggerEvent}</span> },
                { key: "when", label: "Earned", render: a => date(a.earnedAt), nowrap: true },
              ]} />
          </Panel>
        </div>

        <aside className="record-margin">
          <Panel title="Detect achievements">
            <form onSubmit={detect} className="panel-form">
              <label><span>Student number</span><input value={detectStudentId} onChange={e => setDetectStudentId(e.target.value)} placeholder="e.g. 2026F-BSCS-001" required /></label>
              <p className="field-hint">Checks the student&apos;s record against every active badge rule and awards what is due.</p>
              <button type="submit" className={`btn btn-primary${busy === "detect" ? " btn-loading" : ""}`} disabled={busy !== null}>{busy === "detect" ? "Checking" : "Run detection"}</button>
            </form>
          </Panel>
          <Panel title="Process the dean's list">
            <form onSubmit={processDeans} className="panel-form">
              <label><span>Semester</span><input value={deansSemester} onChange={e => setDeansSemester(e.target.value)} placeholder="e.g. Fall 2026" required /></label>
              <p className="field-hint">Lists every student at or above 3.50 on the results recorded for the semester.</p>
              <button type="submit" className={`btn btn-secondary${busy === "deans" ? " btn-loading" : ""}`} disabled={busy !== null}>{busy === "deans" ? "Processing" : "Process semester"}</button>
            </form>
          </Panel>
        </aside>
      </div>
    </div>
  );
}
