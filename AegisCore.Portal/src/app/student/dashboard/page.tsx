"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { RegisterHead, Panel, Facts, Figures, Mark, RuledLoading, ErrorNotice, Empty, ProgressRule } from "@/components/register";
import { FileText, GraduationCap, UserRound, Award } from "lucide-react";

type Overview = {
  student: { id: string; name: string; universityId: string; rollNumber: string; status: string };
  admission: { applicationNumber: string; status: string; approvedDocuments: number; testScore: number; testPassed: boolean } | null;
  academics: { program: string; cgpa: number; earnedCredits: number; requiredCredits: number; completedCourses: number; totalCourses: number; courses: { code: string; title: string; credits: number; grade: string | null; status: string }[] };
  attendance: { course: string; total: number; present: number; percentage: number }[];
  finance: { demanded: number; paid: number; outstanding: number; status: string };
  timetable: { day: string; startTime: string; endTime: string; room: string; course: string }[];
};

export default function StudentDashboard() {
  const { money, date } = useTenantFormatter();
  const [data, setData] = useState<Overview | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [retry, setRetry] = useState(0);

  useEffect(() => {
    const abort = new AbortController();
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reset on retry.
    setError(null);
    apiGet<Overview>("/api/student/overview", abort.signal).then(setData).catch(e => { if (!abort.signal.aborted) setError(e instanceof Error ? e.message : "Your record could not be loaded."); });
    return () => abort.abort();
  }, [retry]);

  const attendance = data?.attendance.length ? data.attendance.reduce((sum, row) => sum + row.percentage, 0) / data.attendance.length : null;
  const feesClear = data ? !data.finance.outstanding : false;

  return (
    <AppShell>
      <div className="page">
        {error ? (
          <>
            <RegisterHead title="Student dashboard" reveal={false} />
            <ErrorNotice message={error} hint="Check your connection and try again." onRetry={() => setRetry(n => n + 1)} />
          </>
        ) : !data ? (
          <RuledLoading label="Loading your verified academic record" rows={6} />
        ) : (
          <>
            <RegisterHead
              title={`Welcome, ${data.student.name}`}
              ident={<><strong>{data.student.rollNumber}</strong><br />{date(new Date())}</>}
              meta={<><Mark tone="laurel">{data.student.status}</Mark><span>{data.academics.program}</span></>}
            />

            <Figures aria-label="Academic summary" items={[
              { label: "CGPA", value: data.academics.cgpa.toFixed(2), note: "on a 4.00 scale" },
              { label: "Attendance", value: attendance == null ? "—" : `${Math.round(attendance)}%`, note: attendance == null ? "no classes recorded" : `across ${data.attendance.length} course${data.attendance.length === 1 ? "" : "s"}`, tone: attendance != null && attendance < 75 ? "oxblood" : undefined },
              { label: "Credits earned", value: `${data.academics.earnedCredits} / ${data.academics.requiredCredits}`, mono: true, tone: data.academics.earnedCredits >= data.academics.requiredCredits ? "laurel" : undefined },
              { label: "Fees", value: feesClear ? (data.finance.status || "Clear") : money(data.finance.outstanding), note: feesClear ? "nothing outstanding" : "outstanding", tone: feesClear ? "laurel" : "oxblood" },
            ]} />

            <div className="record">
              <div className="record-body">
                <Panel title="Latest results" aside={<span>{data.academics.completedCourses} of {data.academics.totalCourses} courses completed</span>} flush
                  foot={<Link href="/student/transcript">Open the full transcript</Link>}>
                  {data.academics.courses.length === 0 ? (
                    <Empty title="No results recorded yet.">Results appear here once the Examinations office publishes them.</Empty>
                  ) : (
                    <div className="ledger-wrap">
                      <table className="ledger" aria-label="Latest results">
                        <thead><tr><th scope="col">Code</th><th scope="col">Course</th><th scope="col" className="num">Credits</th><th scope="col" className="num">Grade</th></tr></thead>
                        <tbody>
                          {data.academics.courses.slice(-6).reverse().map(course => (
                            <tr key={course.code} data-status={course.grade ? "laurel" : "ink"}>
                              <td className="mono">{course.code}</td>
                              <td>{course.title}</td>
                              <td className="num mono">{course.credits}</td>
                              <td className="num mono">{course.grade ?? <span className="text-muted">In progress</span>}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </Panel>

                <Panel title="Degree progress">
                  <ProgressRule value={data.academics.earnedCredits} max={data.academics.requiredCredits} label="Credit hours towards the degree" tone={data.academics.earnedCredits >= data.academics.requiredCredits ? "laurel" : "brass"} />
                  <div className="btn-row">
                    <Link href={`/graduation/${data.student.id}`} className="btn btn-secondary btn-sm"><GraduationCap aria-hidden="true" />Graduation review</Link>
                    <Link href="/alumni" className="btn btn-quiet btn-sm"><Award aria-hidden="true" />Alumni record</Link>
                  </div>
                </Panel>
              </div>

              <aside className="record-margin" aria-label="Student record details">
                <Panel title="Timetable" flush>
                  {data.timetable.length === 0 ? (
                    <Empty title="No classes scheduled.">Your timetable is published by your department each term.</Empty>
                  ) : (
                    <div className="rows" style={{ padding: "0 var(--s-6) var(--s-2)" }}>
                      {data.timetable.slice(0, 5).map(row => (
                        <div key={`${row.day}-${row.course}-${row.startTime}`}>
                          <span><strong>{row.course}</strong><small>{row.day}, {row.room}</small></span>
                          <span className="mono" style={{ fontSize: "var(--t-13)" }}>{row.startTime.slice(0, 5)}–{row.endTime.slice(0, 5)}</span>
                        </div>
                      ))}
                    </div>
                  )}
                </Panel>

                <Panel title="Admission to enrolment">
                  <Facts items={[
                    { label: "Application", value: data.admission?.applicationNumber ?? "Not on record", mono: Boolean(data.admission) },
                    { label: "Documents", value: data.admission ? `${data.admission.approvedDocuments} verified` : "—" },
                    { label: "Entry test", value: data.admission ? <Mark tone={data.admission.testPassed ? "laurel" : "oxblood"}>{data.admission.testScore}%, {data.admission.testPassed ? "passed" : "not passed"}</Mark> : "—" },
                    { label: "Student number", value: data.student.universityId, mono: true },
                  ]} />
                </Panel>

                <div className="btn-row">
                  <Link href="/student/profile" className="btn btn-quiet btn-sm"><UserRound aria-hidden="true" />Student record</Link>
                  <Link href="/student/transcript" className="btn btn-quiet btn-sm"><FileText aria-hidden="true" />Transcript</Link>
                </div>
              </aside>
            </div>
          </>
        )}
      </div>
    </AppShell>
  );
}
