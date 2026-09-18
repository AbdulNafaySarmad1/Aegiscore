"use client";

import { useState } from "react";
import { apiGet } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useModuleData } from "@/components/register/module-page";

type Session = { id: string; examId: string; mode: string; durationMinutes: number; lockdownEnabled: boolean; cameraRequired: boolean; micRequired: boolean; sensitivity: string; scheduledStart: string; actualStart: string | null; actualEnd: string | null; students: number; flagged: number };
type Record_ = { id: string; sessionId: string; studentId: string; studentName: string; integrityStatus: string; warningCount: number; criticalCount: number; verificationStatus: string; isEscalated: boolean; escalatedAt: string | null };
type Event_ = { id: string; recordId: string; severity: string; eventType: string; description: string | null; recordedAt: string };
type Data = { sessions: Session[]; records: Record_[]; events: Event_[] };

/** Proctoring: the invigilated sittings, each student's integrity standing, and the events behind it. */
export default function ProctoringPage() {
  const { date } = useTenantFormatter();
  const [session, setSession] = useState("");
  const { state, reload } = useModuleData<Data>(async signal => {
    const [sessions, records, events] = await Promise.all([
      apiGet<Session[]>("/api/proctoring/sessions", signal),
      apiGet<Record_[]>(`/api/proctoring/integrity${session ? `?sessionId=${session}` : ""}`, signal),
      apiGet<Event_[]>("/api/proctoring/events", signal),
    ]);
    return { sessions, records, events };
  }, session);

  return (
    <ModulePage
      title="Proctoring"
      crumbs={[{ label: "Academic" }, { label: "Proctoring" }]}
      meta={<span>Invigilated sittings, integrity standing and the events recorded against each student.</span>}
      ident={(d: Data) => <><strong>{d.sessions.length}</strong><br />sittings</>}
      state={state} reload={reload}
    >
      {({ sessions, records, events }) => (
        <>
          <Figures aria-label="Proctoring figures" items={[
            { label: "Sittings", value: sessions.length, note: `${sessions.filter(s => s.actualEnd === null && s.actualStart !== null).length} in progress` },
            { label: "Students invigilated", value: records.length, note: sessions.length === 0 ? "none yet" : "across the sittings shown" },
            { label: "Flagged", value: records.filter(r => r.criticalCount > 0).length, tone: records.some(r => r.criticalCount > 0) ? "oxblood" : "ink", note: `${records.filter(r => r.warningCount > 0).length} with warnings` },
            { label: "Escalated", value: records.filter(r => r.isEscalated).length, tone: records.some(r => r.isEscalated) ? "brass" : "ink", note: "to the examinations office" },
          ]} />

          <div className="panel-stack">
            <Panel title="Sittings" flush>
              <Ledger rows={sessions} keyOf={s => s.id} minWidth={780}
                empty={{ title: "No sitting recorded", detail: "A proctoring session is created for an exam with its mode, duration and requirements." }}
                columns={[
                  { key: "exam", label: "Exam", render: s => s.examId, mono: true },
                  { key: "mode", label: "Mode", render: s => words(s.mode) },
                  { key: "when", label: "Scheduled", render: s => date(s.scheduledStart), nowrap: true },
                  { key: "duration", label: "Duration", render: s => `${s.durationMinutes} min`, num: true },
                  { key: "requires", label: "Requires", render: s => [s.lockdownEnabled && "lockdown", s.cameraRequired && "camera", s.micRequired && "microphone"].filter(Boolean).join(", ") || "nothing" },
                  { key: "students", label: "Students", render: s => s.students, num: true },
                  { key: "flagged", label: "Flagged", render: s => s.flagged, num: true },
                ]} />
            </Panel>

            <Panel title="Integrity" aside={
              <label className="toolbar-inline"><span className="sr-only">Sitting</span>
                <select value={session} onChange={e => setSession(e.target.value)}>
                  <option value="">All sittings</option>
                  {sessions.map(s => <option key={s.id} value={s.id}>{date(s.scheduledStart)} — {words(s.mode)}</option>)}
                </select>
              </label>} flush>
              <Ledger rows={records} keyOf={r => r.id} minWidth={720} status={r => r.criticalCount > 0 ? "oxblood" : r.warningCount > 0 ? "brass" : "laurel"}
                empty={{ title: "No student invigilated", detail: "A record is created for each student when they enter the sitting." }}
                columns={[
                  { key: "student", label: "Student", render: r => r.studentName },
                  { key: "id", label: "Student number", render: r => r.studentId, mono: true },
                  { key: "status", label: "Integrity", render: r => <Mark tone={toneOf(r.integrityStatus)}>{words(r.integrityStatus)}</Mark> },
                  { key: "warnings", label: "Warnings", render: r => r.warningCount, num: true },
                  { key: "critical", label: "Critical", render: r => r.criticalCount, num: true },
                  { key: "verification", label: "Environment", render: r => words(r.verificationStatus) },
                  { key: "escalated", label: "Escalated", render: r => r.isEscalated ? date(r.escalatedAt ?? "") : "no", nowrap: true },
                ]} />
            </Panel>

            <Panel title="Events" aside={<span className="text-muted">most recent first</span>} flush>
              <Ledger rows={events} keyOf={e => e.id} minWidth={640} status={e => toneOf(e.severity)}
                empty={{ title: "No events recorded", detail: "Events are logged by the invigilation client during a sitting." }}
                columns={[
                  { key: "when", label: "Recorded", render: e => date(e.recordedAt), nowrap: true },
                  { key: "severity", label: "Severity", render: e => <Mark tone={toneOf(e.severity)}>{words(e.severity)}</Mark> },
                  { key: "type", label: "Event", render: e => words(e.eventType) },
                  { key: "desc", label: "What happened", render: e => e.description ?? "—" },
                ]} />
            </Panel>
          </div>
        </>
      )}
    </ModulePage>
  );
}
