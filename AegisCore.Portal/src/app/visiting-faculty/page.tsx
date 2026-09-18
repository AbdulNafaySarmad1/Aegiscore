"use client";

import { apiGet } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useModuleData } from "@/components/register/module-page";

type Profile = { id: string; name: string; institution: string | null; degreeTitle: string | null; specializations: string | null; yearsTeaching: number; yearsIndustry: number; hecEligibility: string | null; status: string; documentsVerified: boolean; referencesChecked: boolean; perCreditHourRate: number };
type Match = { id: string; slotId: string; facultyName: string; compositeScore: number; rank: number; isSelected: boolean; matchedAt: string };
type Contract = { id: string; facultyName: string; course: string; status: string; creditHours: number; compensationAmount: number; startDate: string; endDate: string; signedAt: string | null; rehireStatus: string | null; performanceRating: number | null };
type Data = { profiles: Profile[]; matches: Match[]; contracts: Contract[] };

/** Visiting faculty: the pool, how it was matched to open teaching slots, and the contracts issued. */
export default function VisitingFacultyPage() {
  const { date, money } = useTenantFormatter();
  const { state, reload } = useModuleData<Data>(async signal => {
    const [profiles, matches, contracts] = await Promise.all([
      apiGet<Profile[]>("/api/visiting-faculty/list", signal),
      apiGet<Match[]>("/api/visiting-faculty/matches", signal),
      apiGet<Contract[]>("/api/visiting-faculty/contracts", signal),
    ]);
    return { profiles, matches, contracts };
  });

  return (
    <ModulePage
      title="Visiting faculty"
      crumbs={[{ label: "Human resources" }, { label: "Visiting faculty" }]}
      meta={<span>The vetted pool, slot matches and issued contracts.</span>}
      ident={(d: Data) => <><strong>{d.profiles.length}</strong><br />in the pool</>}
      state={state} reload={reload}
    >
      {({ profiles, matches, contracts }) => (
        <>
          <Figures aria-label="Visiting faculty figures" items={[
            { label: "In the pool", value: profiles.length, note: `${profiles.filter(p => p.documentsVerified).length} with documents verified` },
            { label: "Ready to deploy", value: profiles.filter(p => /ready/i.test(p.status)).length, note: "vetting complete" },
            { label: "Slot matches", value: matches.length, note: `${new Set(matches.map(m => m.slotId)).size} slots matched` },
            { label: "Contracts", value: contracts.length, note: `${contracts.filter(c => c.signedAt).length} signed` },
          ]} />

          <div className="panel-stack">
            <Panel title="Pool" aside={<span className="text-muted">{profiles.length} profiles</span>} flush>
              <Ledger rows={profiles} keyOf={p => p.id} minWidth={860} status={p => toneOf(p.status)}
                empty={{ title: "Nobody in the pool", detail: "A visiting-faculty profile records qualifications, experience and the HEC eligibility check." }}
                columns={[
                  { key: "name", label: "Name", render: p => <strong>{p.name}</strong> },
                  { key: "institution", label: "Institution", render: p => p.institution ?? "—" },
                  { key: "degree", label: "Highest degree", render: p => p.degreeTitle ?? "—" },
                  { key: "specialisation", label: "Specialisations", render: p => p.specializations ?? "—" },
                  { key: "teaching", label: "Teaching yrs", render: p => p.yearsTeaching, num: true },
                  { key: "industry", label: "Industry yrs", render: p => p.yearsIndustry, num: true },
                  { key: "hec", label: "HEC", render: p => words(p.hecEligibility) },
                  { key: "rate", label: "Per credit hour", render: p => money(Number(p.perCreditHourRate)), num: true },
                  { key: "status", label: "Vetting", render: p => <Mark tone={toneOf(p.status)}>{words(p.status)}</Mark> },
                ]} />
            </Panel>

            <Panel title="Slot matches" aside={<span className="text-muted">ranked by composite score</span>} flush>
              <Ledger rows={matches} keyOf={m => m.id} minWidth={640} status={m => m.isSelected ? "laurel" : "ink"}
                empty={{ title: "No slot matched", detail: "Matching ranks the ready-to-deploy pool against an open teaching slot." }}
                columns={[
                  { key: "slot", label: "Open slot", render: m => m.slotId, mono: true },
                  { key: "rank", label: "Rank", render: m => m.rank, num: true },
                  { key: "faculty", label: "Candidate", render: m => m.facultyName },
                  { key: "score", label: "Score", render: m => m.compositeScore.toFixed(2), num: true },
                  { key: "selected", label: "Selected", render: m => m.isSelected ? <Mark tone="laurel">Selected</Mark> : "—" },
                  { key: "when", label: "Matched", render: m => date(m.matchedAt), nowrap: true },
                ]} />
            </Panel>

            <Panel title="Contracts" aside={<span className="text-muted">{contracts.length} issued</span>} flush>
              <Ledger rows={contracts} keyOf={c => c.id} minWidth={800} status={c => toneOf(c.status)}
                empty={{ title: "No contracts issued", detail: "A contract is generated once a candidate is selected for a course." }}
                columns={[
                  { key: "faculty", label: "Faculty", render: c => c.facultyName },
                  { key: "course", label: "Course", render: c => c.course || "—" },
                  { key: "credits", label: "Credit hours", render: c => c.creditHours, num: true },
                  { key: "amount", label: "Compensation", render: c => money(Number(c.compensationAmount)), num: true },
                  { key: "term", label: "Term", render: c => `${date(c.startDate)} – ${date(c.endDate)}`, nowrap: true },
                  { key: "status", label: "Status", render: c => <Mark tone={toneOf(c.status)}>{words(c.status)}</Mark> },
                  { key: "rating", label: "Rating", render: c => c.performanceRating === null ? "—" : c.performanceRating.toFixed(1), num: true },
                ]} />
            </Panel>
          </div>
        </>
      )}
    </ModulePage>
  );
}
