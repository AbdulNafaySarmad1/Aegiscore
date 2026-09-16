"use client";
import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost } from "@/lib/api-client";
import { CheckCircle, Send } from "lucide-react";
import { RegisterHead, Panel, Figures, Mark, RuledLoading, ErrorNotice, Notice, useAnnounce } from "@/components/register";
import { TranscriptDownload } from "@/components/documents/TranscriptDownload";
import { useInstitution } from "@/components/TenantPreferencesProvider";

type Course = { code: string; title: string; credits: number; grade: string | null; status: string };
type Overview = { student: { id: string; name: string; rollNumber: string; universityId?: string }; academics: { program: string; cgpa: number; earnedCredits: number; requiredCredits: number; courses: Course[] } };

const gradeTone = (grade: string | null) => grade == null ? "ink" : /^F/i.test(grade) ? "oxblood" : "laurel";

export default function TranscriptPage() {
  const [data, setData] = useState<Overview | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [requestState, setRequestState] = useState<"idle" | "sending" | "requested">("idle");
  const [requestError, setRequestError] = useState<string | null>(null);
  const [retry, setRetry] = useState(0);
  const { announce, region } = useAnnounce();
  const institution = useInstitution();

  useEffect(() => {
    const abort = new AbortController();
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reset on retry.
    setError(null);
    apiGet<Overview>("/api/student/overview", abort.signal).then(setData).catch(e => { if (!abort.signal.aborted) setError(e instanceof Error ? e.message : "Your record could not be loaded."); });
    return () => abort.abort();
  }, [retry]);

  async function requestOfficial() {
    if (!data) return;
    setRequestState("sending"); setRequestError(null);
    try {
      await apiPost(`/api/examination/transcripts/${data.student.id}/request`, { requestedBy: data.student.id, reason: "Official student copy" });
      setRequestState("requested");
      announce("Official copy requested from the Examinations office.");
    } catch (e) {
      setRequestState("idle");
      setRequestError(e instanceof Error ? e.message : "The request could not be sent.");
    }
  }

  const completed = data?.academics.courses.filter(c => c.grade).length ?? 0;

  return (
    <AppShell>
      <div className="page">
        {region}
        {error ? (
          <>
            <RegisterHead title="Academic transcript" crumbs={[{ label: "Student" }, { label: "Transcript" }]} reveal={false} />
            <ErrorNotice message={error} hint="Check your connection and try again." onRetry={() => setRetry(n => n + 1)} />
          </>
        ) : !data ? (
          <RuledLoading label="Loading your verified record" rows={8} />
        ) : (
          <>
            <RegisterHead
              title="Academic transcript"
              crumbs={[{ label: "Student" }, { label: "Transcript" }]}
              ident={<><strong>{data.student.rollNumber}</strong><br />{data.student.name}</>}
              meta={<span>{data.academics.program}</span>}
              actions={<>
                <button type="button" className={`btn btn-secondary${requestState === "sending" ? " btn-loading" : ""}`} disabled={requestState !== "idle"} onClick={requestOfficial}>
                  {requestState === "requested" ? <><CheckCircle aria-hidden="true" />Official copy requested</> : <><Send aria-hidden="true" />Request official copy</>}
                </button>
                <TranscriptDownload fileName={`transcript-${data.student.rollNumber}.pdf`} data={() => ({
                  institution: { name: institution.name, logoUrl: institution.logoUrl, sealUrl: institution.sealUrl },
                  student: { name: data.student.name, rollNumber: data.student.rollNumber, universityId: data.student.universityId, program: data.academics.program },
                  academics: data.academics,
                })} />
              </>}
            />

            {requestError && <ErrorNotice message={requestError} hint="Nothing was sent. Try again, or contact the Examinations office directly." />}
            {requestState === "requested" && <Notice tone="laurel" title="Official copy requested.">The Examinations office will issue a sealed transcript and notify you at your institutional email.</Notice>}

            <Figures aria-label="Academic summary" items={[
              { label: "CGPA", value: data.academics.cgpa.toFixed(2), note: "on a 4.00 scale" },
              { label: "Credits earned", value: data.academics.earnedCredits, note: `of ${data.academics.requiredCredits} required` },
              { label: "Courses completed", value: completed, note: `of ${data.academics.courses.length} on record` },
              { label: "In progress", value: data.academics.courses.length - completed },
            ]} />

            <Panel title="Record of courses" aside={<span>Grades on the university&rsquo;s approved scale</span>} flush>
              <div className="ledger-wrap">
                <table className="ledger" aria-label="Courses and grades">
                  <thead><tr><th scope="col">Code</th><th scope="col">Course</th><th scope="col" className="num">Credits</th><th scope="col" className="num">Grade</th><th scope="col">Status</th></tr></thead>
                  <tbody>
                    {data.academics.courses.map(c => (
                      <tr key={c.code} data-status={gradeTone(c.grade)}>
                        <td className="mono">{c.code}</td>
                        <td>{c.title}</td>
                        <td className="num mono">{c.credits}</td>
                        <td className="num mono">{c.grade ?? <span className="text-muted">—</span>}</td>
                        <td><Mark tone={c.grade ? gradeTone(c.grade) : "ink"}>{c.status}</Mark></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Panel>
          </>
        )}
      </div>
    </AppShell>
  );
}
