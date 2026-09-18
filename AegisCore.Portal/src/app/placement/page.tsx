"use client";

import { apiGet } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useModuleData } from "@/components/register/module-page";

type Partner = { id: string; name: string; description: string | null; contactEmail: string | null; status: string; openings: number; placed: number };
type Posting = { id: string; title: string; employer: string; jobType: string; location: string | null; status: string; postedAt: string; expiresAt: string | null; applications: number };
type Analytics = { placed: number; graduates: number; openings: number; employers: number; placementRate: number | null; medianSalary: number | null; byType: { type: string; count: number }[] };
type Data = { partners: Partner[]; postings: Posting[]; analytics: Analytics };

/** Placement: the employers on the register, what they have open, and how many graduates were placed. */
export default function PlacementPage() {
  const { date, money } = useTenantFormatter();
  const { state, reload } = useModuleData<Data>(async signal => {
    const [partners, postings, analytics] = await Promise.all([
      apiGet<Partner[]>("/api/placement/partners", signal),
      apiGet<Posting[]>("/api/placement/active", signal),
      apiGet<Analytics>("/api/placement/analytics", signal),
    ]);
    return { partners, postings, analytics };
  });

  return (
    <ModulePage
      title="Placement"
      crumbs={[{ label: "Student welfare" }, { label: "Placement" }]}
      meta={<span>Employers, openings and placements, as recorded.</span>}
      ident={(d: Data) => <><strong>{d.analytics.placed}</strong><br />students placed</>}
      state={state} reload={reload}
    >
      {({ partners, postings, analytics }) => (
        <>
          <Figures aria-label="Placement figures" items={[
            { label: "Students placed", value: analytics.placed, note: analytics.graduates === 0 ? "no graduates recorded yet" : `of ${analytics.graduates} graduates` },
            { label: "Placement rate", value: analytics.placementRate === null ? "—" : `${analytics.placementRate}%`, note: analytics.placementRate === null ? "needs graduates on the register" : "of graduates placed" },
            { label: "Open positions", value: analytics.openings, note: `${analytics.employers} employers` },
            { label: "Median salary", value: analytics.medianSalary === null ? "—" : money(Number(analytics.medianSalary)), note: analytics.medianSalary === null ? "no salary recorded" : "of placements with a salary" },
          ]} />

          <div className="panel-stack">
            <Panel title="Employers" aside={<span className="text-muted">{partners.length} on the register</span>} flush>
              <Ledger rows={partners} keyOf={p => p.id} minWidth={680} status={p => toneOf(p.status)}
                empty={{ title: "No employers registered", detail: "The placement office records each employer it works with." }}
                columns={[
                  { key: "name", label: "Employer", render: p => <strong>{p.name}</strong> },
                  { key: "contact", label: "Contact", render: p => p.contactEmail ?? "—" },
                  { key: "status", label: "Status", render: p => <Mark tone={toneOf(p.status)}>{words(p.status)}</Mark> },
                  { key: "openings", label: "Open roles", render: p => p.openings, num: true },
                  { key: "placed", label: "Placed", render: p => p.placed, num: true },
                ]} />
            </Panel>

            <Panel title="Openings" aside={<span className="text-muted">{postings.length} postings</span>} flush>
              <Ledger rows={postings} keyOf={p => p.id} minWidth={720} status={p => toneOf(p.status)}
                empty={{ title: "No openings posted", detail: "Employers' vacancies appear here once the placement office records them." }}
                columns={[
                  { key: "title", label: "Role", render: p => <strong>{p.title}</strong> },
                  { key: "employer", label: "Employer", render: p => p.employer },
                  { key: "type", label: "Type", render: p => words(p.jobType) },
                  { key: "location", label: "Location", render: p => p.location ?? "—" },
                  { key: "applications", label: "Applications", render: p => p.applications, num: true },
                  { key: "posted", label: "Posted", render: p => date(p.postedAt), nowrap: true },
                  { key: "status", label: "Status", render: p => <Mark tone={toneOf(p.status)}>{words(p.status)}</Mark> },
                ]} />
            </Panel>

            <Panel title="Placements by type" flush>
              <Ledger rows={analytics.byType} keyOf={t => t.type} minWidth={320}
                empty={{ title: "No placements recorded", detail: "A placement is recorded when a student accepts an offer." }}
                columns={[
                  { key: "type", label: "Type", render: t => words(t.type) },
                  { key: "count", label: "Students", render: t => t.count, num: true },
                ]} />
            </Panel>
          </div>
        </>
      )}
    </ModulePage>
  );
}
