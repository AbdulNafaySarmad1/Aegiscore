"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { RegisterHead, Panel, Facts, Mark, RuledLoading, ErrorNotice } from "@/components/register";

type Overview = {
  student: { name: string; universityId: string; rollNumber: string; institutionalEmail: string; status: string };
  admission: { applicationNumber: string; email: string; status: string } | null;
  academics: { program: string; cgpa: number; earnedCredits: number; requiredCredits: number };
  finance: { status: string; outstanding: number };
};

export default function StudentProfile() {
  const { money } = useTenantFormatter();
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

  const enrolled = data ? /enrol/i.test(data.student.status) || /active/i.test(data.student.status) : false;
  const feesClear = data ? !data.finance.outstanding : false;

  return (
    <AppShell>
      <div className="page page-narrow">
        {error ? (
          <>
            <RegisterHead title="Student record" crumbs={[{ label: "Student" }, { label: "Profile" }]} reveal={false} />
            <ErrorNotice message={error} hint="Check your connection and try again." onRetry={() => setRetry(n => n + 1)} />
          </>
        ) : !data ? (
          <RuledLoading label="Opening your student record" rows={6} />
        ) : (
          <>
            <RegisterHead
              title={data.student.name}
              crumbs={[{ label: "Student" }, { label: "Profile" }]}
              ident={<><strong>{data.student.universityId}</strong><br />Student number</>}
              meta={<><Mark tone={enrolled ? "laurel" : "brass"}>{data.student.status}</Mark><span>{data.academics.program}</span></>}
              actions={<><Link href="/student/transcript" className="btn btn-secondary">Open transcript</Link></>}
            />

            <Panel title="Identity" aside={<span>As held by the Registrar</span>}>
              <Facts items={[
                { label: "Full name", value: data.student.name },
                { label: "Student number", value: data.student.universityId, mono: true },
                { label: "Roll number", value: data.student.rollNumber, mono: true },
                { label: "Institutional email", value: data.student.institutionalEmail, mono: true },
                { label: "Enrolment status", value: <Mark tone={enrolled ? "laurel" : "brass"}>{data.student.status}</Mark> },
              ]} />
            </Panel>

            <Panel title="Academic standing">
              <Facts items={[
                { label: "Programme", value: data.academics.program },
                { label: "CGPA", value: `${data.academics.cgpa.toFixed(2)} / 4.00`, mono: true },
                { label: "Credit hours", value: `${data.academics.earnedCredits} / ${data.academics.requiredCredits}`, mono: true },
              ]} />
            </Panel>

            <Panel title="Admission and fees">
              <Facts items={[
                { label: "Application", value: data.admission?.applicationNumber ?? "Not on record", mono: Boolean(data.admission) },
                { label: "Admission status", value: data.admission?.status ?? "Not on record" },
                { label: "Fee status", value: feesClear
                  ? <Mark tone="laurel">{data.finance.status || "Clear"}</Mark>
                  : <Mark tone="oxblood">{money(data.finance.outstanding)} outstanding</Mark> },
              ]} />
            </Panel>
          </>
        )}
      </div>
    </AppShell>
  );
}
