"use client";

import { useState } from "react";
import { apiGet } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark, Notice } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useModuleData } from "@/components/register/module-page";

type Election = { id: string; title: string; electionType: string; votingMethod: string; nominationStart: string; nominationEnd: string; votingStart: string; votingEnd: string; status: string; turnoutRate: number | null; resultsPublishedAt: string | null; positions: number; candidates: number; ballots: number };
type Candidate = { id: string; electionId: string; position: string; studentName: string; studentId: string; endorsementCount: number; status: string; eligibilityVerified: boolean };
type Result = { id: string; electionId: string; position: string; candidate: string; voteCount: number; isElected: boolean };
type Data = { elections: Election[]; candidates: Candidate[]; results: Result[] };

/** Student elections: the timetable, the nominations, the ballots cast and the results certified. */
export default function ElectionsPage() {
  const { date } = useTenantFormatter();
  const [election, setElection] = useState("");
  const { state, reload } = useModuleData<Data>(async signal => {
    const [elections, candidates, results] = await Promise.all([
      apiGet<Election[]>("/api/elections", signal),
      apiGet<Candidate[]>(`/api/elections/candidates${election ? `?electionId=${election}` : ""}`, signal),
      apiGet<Result[]>(`/api/elections/results${election ? `?electionId=${election}` : ""}`, signal),
    ]);
    return { elections, candidates, results };
  }, election);

  return (
    <ModulePage
      title="Elections"
      crumbs={[{ label: "Campus operations" }, { label: "Elections" }]}
      meta={<span>Ballots are stored encrypted; a count appears here only once the results have been tallied and certified.</span>}
      ident={(d: Data) => <><strong>{d.elections.length}</strong><br />elections</>}
      state={state} reload={reload}
    >
      {({ elections, candidates, results }) => (
        <div className="panel-stack">
          <Figures aria-label="Election figures" items={[
            { label: "Elections", value: elections.length, note: elections.filter(e => e.status !== "completed").length + " open" },
            { label: "Candidates", value: candidates.length, note: `${candidates.filter(c => c.eligibilityVerified).length} verified eligible` },
            { label: "Ballots cast", value: elections.reduce((n, e) => n + e.ballots, 0), note: "held encrypted until tallied" },
            { label: "Results certified", value: results.filter(r => r.isElected).length, note: results.length === 0 ? "none tallied" : `${results.length} positions counted` },
          ]} />

          {elections.some(e => e.ballots > 0) && results.length === 0 && (
            <Notice tone="brass" title="Ballots are cast but not tallied." role="status">
              Tallying needs the election&apos;s key; until it is run, no count exists to show — the ballots are stored encrypted.
            </Notice>
          )}

          <Panel title="Elections" flush>
            <Ledger rows={elections} keyOf={e => e.id} minWidth={840} status={e => toneOf(e.status)}
              empty={{ title: "No election recorded", detail: "An election records its nomination, campaigning and voting windows before anything else happens." }}
              columns={[
                { key: "title", label: "Election", render: e => <strong>{e.title}</strong> },
                { key: "type", label: "Type", render: e => words(e.electionType) },
                { key: "nominations", label: "Nominations", render: e => `${date(e.nominationStart)} – ${date(e.nominationEnd)}`, nowrap: true },
                { key: "voting", label: "Voting", render: e => `${date(e.votingStart)} – ${date(e.votingEnd)}`, nowrap: true },
                { key: "positions", label: "Positions", render: e => e.positions, num: true },
                { key: "candidates", label: "Candidates", render: e => e.candidates, num: true },
                { key: "ballots", label: "Ballots", render: e => e.ballots, num: true },
                { key: "turnout", label: "Turnout", render: e => e.turnoutRate === null ? "—" : `${e.turnoutRate}%`, num: true },
                { key: "status", label: "Status", render: e => <Mark tone={toneOf(e.status)}>{words(e.status)}</Mark> },
              ]} />
          </Panel>

          <Panel title="Candidates" aside={
            <label className="toolbar-inline"><span className="sr-only">Election</span>
              <select value={election} onChange={e => setElection(e.target.value)}>
                <option value="">All elections</option>
                {elections.map(e => <option key={e.id} value={e.id}>{e.title}</option>)}
              </select>
            </label>} flush>
            <Ledger rows={candidates} keyOf={c => c.id} minWidth={700} status={c => toneOf(c.status)}
              empty={{ title: "No nominations", detail: "Students nominate themselves for a position during the nomination window." }}
              columns={[
                { key: "position", label: "Position", render: c => c.position || "—" },
                { key: "candidate", label: "Candidate", render: c => c.studentName },
                { key: "id", label: "Student number", render: c => c.studentId, mono: true },
                { key: "endorsements", label: "Endorsements", render: c => c.endorsementCount, num: true },
                { key: "eligible", label: "Eligibility", render: c => c.eligibilityVerified ? <Mark tone="laurel">Verified</Mark> : <Mark tone="brass">Unverified</Mark> },
                { key: "status", label: "Nomination", render: c => <Mark tone={toneOf(c.status)}>{words(c.status)}</Mark> },
              ]} />
          </Panel>

          <Panel title="Results" flush>
            <Ledger rows={results} keyOf={r => r.id} minWidth={560} status={r => r.isElected ? "laurel" : "ink"}
              empty={{ title: "No results", detail: "Results appear once an election has been tallied and the count certified." }}
              columns={[
                { key: "position", label: "Position", render: r => r.position || "—" },
                { key: "candidate", label: "Candidate", render: r => r.candidate || "—" },
                { key: "votes", label: "Votes", render: r => r.voteCount, num: true },
                { key: "elected", label: "Outcome", render: r => r.isElected ? <Mark tone="laurel">Elected</Mark> : "—" },
              ]} />
          </Panel>
        </div>
      )}
    </ModulePage>
  );
}
