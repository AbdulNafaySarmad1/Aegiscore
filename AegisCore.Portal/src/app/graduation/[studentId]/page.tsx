"use client";

import { release } from "@/lib/api-client";
import { use, useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { loadSession } from "@/lib/session";
import { AppShell } from "@/components/layout/AppShell";
import { RegisterHead, Panel, Facts, Figures, Mark, Seal, Requirements, ProgressRule, RuledLoading, ErrorNotice, Notice, useAnnounce } from "@/components/register";

type Progress = { studentName: string; program: string | null; cgpa: number; earnedCredits: number; requiredCredits: number; approved: boolean; blockers: string[] };

const CLEARANCES = ["Library", "Finance", "Department"] as const;
const APPROVER_ROLES = ["admin", "superadmin"];

export default function GraduationPage({ params }: { params: Promise<{ studentId: string }> }) {
  const { studentId } = use(params);
  const [progress, setProgress] = useState<Progress | null>(null);
  const [canApprove, setCanApprove] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [justApproved, setJustApproved] = useState(false);
  const [retry, setRetry] = useState(0);
  const { announce, region } = useAnnounce();

  const refresh = useCallback(async () => {
    const [response, session] = await Promise.all([
      fetch(`/api/graduation/${encodeURIComponent(studentId)}`, { credentials: "include", cache: "no-store" }),
      loadSession()
    ]);
    if (!response.ok) { await release(response); throw new Error(response.status === 403
      ? "You do not have access to this student's graduation record."
      : response.status === 404 ? "No graduation record exists for this student."
      : "The graduation record could not be loaded."); }
    setProgress(await response.json());
    setCanApprove(session.roles.some(role => APPROVER_ROLES.includes(role.toLowerCase())));
  }, [studentId]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial record hydration.
    setLoadError(null);
    refresh().catch(error => setLoadError(error instanceof Error ? error.message : "The graduation record could not be loaded."));
  }, [refresh, retry]);

  async function submit(action: string, done: string) {
    setBusy(action); setActionError(null);
    try {
      const response = await fetch(`/api/graduation/${encodeURIComponent(studentId)}/${action}`, { method: "POST", credentials: "include" });
      if (!response.ok) {
        const body = await response.json().catch(() => ({}));
        throw new Error(response.status === 403 ? "Only an authorised university approver can record this."
          : response.status === 409 ? (body.error ?? "Outstanding requirements must be cleared before graduation can be approved.")
          : body.error ?? "The action could not be recorded.");
      }
      if (action === "approve") setJustApproved(true);
      await refresh();
      announce(done);
    } catch (error) { setActionError(error instanceof Error ? error.message : "The action could not be recorded."); }
    finally { setBusy(null); }
  }

  const clearanceState = useMemo(() => {
    const blockers = progress?.blockers ?? [];
    return CLEARANCES.map(department => {
      const blocker = blockers.find(b => b.toLowerCase().startsWith(`${department.toLowerCase()} clearance`));
      return { department, cleared: !blocker, blocker };
    });
  }, [progress]);
  const otherBlockers = useMemo(() => (progress?.blockers ?? []).filter(b => !CLEARANCES.some(d => b.toLowerCase().startsWith(`${d.toLowerCase()} clearance`))), [progress]);

  return (
    <AppShell>
      <div className="page">
        {region}
        {loadError ? (
          <>
            <RegisterHead title="Graduation review" crumbs={[{ label: "Registrar" }, { label: "Graduation review" }]} reveal={false} />
            <ErrorNotice message={loadError} hint="If you expected access, ask the registrar's office to confirm your role." onRetry={() => setRetry(n => n + 1)} />
          </>
        ) : !progress ? (
          <RuledLoading label="Loading graduation requirements" rows={4} />
        ) : (
          <>
            <RegisterHead
              title={progress.studentName}
              crumbs={[{ label: "Registrar" }, { label: "Graduation review" }]}
              ident={<><strong>{studentId}</strong><br />Graduation record</>}
              meta={<>
                <Mark tone={progress.approved ? "laurel" : progress.blockers.length ? "brass" : "ink"}>{progress.approved ? "Graduation approved" : progress.blockers.length ? `${progress.blockers.length} requirement${progress.blockers.length === 1 ? "" : "s"} outstanding` : "Ready for approval"}</Mark>
                {progress.program && <span>{progress.program}</span>}
              </>}
            />

            {actionError && <ErrorNotice message={actionError} hint="The record was not changed." />}

            <Figures aria-label="Academic standing" items={[
              { label: "CGPA", value: progress.cgpa.toFixed(2), note: "on a 4.00 scale" },
              { label: "Credits earned", value: progress.earnedCredits, note: `of ${progress.requiredCredits} required`, tone: progress.earnedCredits >= progress.requiredCredits ? "laurel" : undefined },
              { label: "Credits remaining", value: Math.max(0, progress.requiredCredits - progress.earnedCredits), tone: progress.earnedCredits >= progress.requiredCredits ? undefined : "brass" },
            ]} />

            <div className="record">
              <div className="record-body">
                {progress.approved ? (
                  <Panel sealed>
                    <div className="sealed">
                      <Seal tone="laurel" stamped={justApproved} label="University seal: graduation approved" />
                      <div>
                        <h2>Graduation approved.</h2>
                        <p>All clearances were recorded and the university approved this graduation. The student may now open their alumni record.</p>
                        <p className="sealed-meta">{studentId}<br />{progress.program ?? "Programme on record"}, CGPA {progress.cgpa.toFixed(2)}</p>
                        <p style={{ marginTop: "var(--s-3)" }}><Link href="/alumni" className="btn btn-secondary btn-sm">Open alumni record</Link></p>
                      </div>
                    </div>
                  </Panel>
                ) : (
                  <>
                    <Panel title="Clearances" aside={<span>{clearanceState.filter(c => c.cleared).length} of {CLEARANCES.length} recorded</span>} foot={canApprove ? <span>Each clearance is attributed to you as the signed-in approver.</span> : undefined}>
                      <Requirements items={clearanceState.map(c => ({
                        label: c.cleared ? `${c.department} clearance recorded` : c.blocker ?? `${c.department} clearance is required.`,
                        state: c.cleared ? "met" : "blocked",
                        action: !c.cleared && canApprove
                          ? <button type="button" className={`btn btn-laurel btn-sm${busy === `clearances/${c.department}` ? " btn-loading" : ""}`} disabled={busy !== null} onClick={() => submit(`clearances/${c.department}`, `${c.department} clearance recorded.`)}>Approve {c.department} clearance</button>
                          : undefined,
                      }))} />
                    </Panel>

                    <Panel title="Academic requirements">
                      <ProgressRule value={progress.earnedCredits} max={progress.requiredCredits} label="Credit hours" tone={progress.earnedCredits >= progress.requiredCredits ? "laurel" : "brass"} />
                      {otherBlockers.length > 0 ? (
                        <Requirements items={otherBlockers.map(b => ({ label: b, state: "blocked" as const }))} />
                      ) : (
                        <Requirements items={[{ label: "All configured academic requirements are satisfied.", state: "met" as const }]} />
                      )}
                    </Panel>
                  </>
                )}
              </div>

              <aside className="record-margin" aria-label="Graduation record details">
                <Panel title="Record">
                  <Facts items={[
                    { label: "Student", value: progress.studentName },
                    { label: "Student ID", value: studentId, mono: true },
                    { label: "Programme", value: progress.program ?? "Not recorded" },
                    { label: "CGPA", value: progress.cgpa.toFixed(2), mono: true },
                    { label: "Credits", value: `${progress.earnedCredits} / ${progress.requiredCredits}`, mono: true },
                    { label: "Decision", value: <Mark tone={progress.approved ? "laurel" : "ink"}>{progress.approved ? "Approved" : "Pending"}</Mark> },
                  ]} />
                </Panel>

                {!progress.approved && (
                  <Panel title="University approval" tone={canApprove && progress.blockers.length === 0 ? "laurel" : undefined}>
                    {canApprove ? (
                      <>
                        <p className="text-muted" style={{ fontSize: "var(--t-13)" }}>
                          {progress.blockers.length > 0
                            ? `Approval opens once the ${progress.blockers.length} outstanding requirement${progress.blockers.length === 1 ? " is" : "s are"} cleared.`
                            : "Every requirement is cleared. Approving records the decision under your name."}
                        </p>
                        <button type="button" className={`btn btn-primary btn-block${busy === "approve" ? " btn-loading" : ""}`} disabled={busy !== null || progress.blockers.length > 0} onClick={() => submit("approve", "Graduation approval recorded.")}>Approve graduation</button>
                      </>
                    ) : (
                      <Notice tone="ink">University staff must complete the listed clearances before graduation can be approved.</Notice>
                    )}
                  </Panel>
                )}
              </aside>
            </div>
          </>
        )}
      </div>
    </AppShell>
  );
}
