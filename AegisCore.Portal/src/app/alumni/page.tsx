"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { AppShell } from "@/components/layout/AppShell";
import { RegisterHead, Panel, Facts, Seal, RuledLoading, Notice, ErrorNotice, Mark } from "@/components/register";

type Profile = { alumniId: string; graduationYear: number; currentCompany?: string; bio?: string; userId?: string };
type State = { kind: "loading" } | { kind: "ready"; profile: Profile } | { kind: "not-graduated" } | { kind: "signed-out" } | { kind: "error"; message: string };

export default function AlumniPage() {
  const [state, setState] = useState<State>({ kind: "loading" });
  const [retry, setRetry] = useState(0);

  useEffect(() => {
    let active = true;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reset on retry.
    setState({ kind: "loading" });
    fetch("/api/student-lifecycle/alumni/me", { credentials: "include", cache: "no-store" })
      .then(async response => {
        if (response.status === 404) return { kind: "not-graduated" } as const;
        if (response.status === 401) return { kind: "signed-out" } as const;
        if (!response.ok) return { kind: "error", message: "Your alumni record could not be loaded." } as const;
        return { kind: "ready", profile: await response.json() as Profile } as const;
      })
      .catch(() => ({ kind: "error", message: "The university API is not reachable." } as const))
      .then(next => { if (active) setState(next); });
    return () => { active = false; };
  }, [retry]);

  return (
    <AppShell>
      <div className="page page-narrow">
        {state.kind === "loading" && <RuledLoading label="Opening your alumni record" rows={4} />}

        {state.kind === "not-graduated" && (
          <>
            <RegisterHead title="Alumni record" crumbs={[{ label: "Student" }, { label: "Alumni" }]} reveal={false} />
            <Panel title="Not yet issued">
              <p>Your alumni record is created when the university approves your graduation. Until then, your degree progress is tracked on the graduation review.</p>
              <div className="btn-row"><Link href="/student/dashboard" className="btn btn-secondary">Back to your dashboard</Link></div>
            </Panel>
          </>
        )}

        {state.kind === "signed-out" && (
          <>
            <RegisterHead title="Alumni record" reveal={false} />
            <Notice tone="brass" title="Sign in to view your alumni record." actions={<Link href="/login" className="btn btn-primary btn-sm">Sign in</Link>} />
          </>
        )}

        {state.kind === "error" && (
          <>
            <RegisterHead title="Alumni record" reveal={false} />
            <ErrorNotice message={state.message} hint="Check your connection and try again." onRetry={() => setRetry(n => n + 1)} />
          </>
        )}

        {state.kind === "ready" && (
          <>
            <RegisterHead
              title="Alumni record"
              crumbs={[{ label: "Student" }, { label: "Alumni" }]}
              ident={<><strong>Class of {state.profile.graduationYear}</strong><br />Alumni record</>}
              meta={<Mark tone="laurel">Graduation approved</Mark>}
            />
            <Panel sealed>
              <div className="sealed">
                <Seal tone="laurel" label="University seal" />
                <div>
                  <h2>Graduation approved.</h2>
                  <p>This record confirms that the university approved your graduation. It remains on file and is available to you after you leave.</p>
                </div>
              </div>
            </Panel>
            <Panel title="Record">
              <Facts items={[
                { label: "Alumni number", value: state.profile.alumniId, mono: true },
                { label: "Graduation year", value: String(state.profile.graduationYear), mono: true },
                ...(state.profile.currentCompany ? [{ label: "Current employer", value: state.profile.currentCompany }] : []),
                ...(state.profile.bio ? [{ label: "About", value: state.profile.bio }] : []),
              ]} />
            </Panel>
            {!state.profile.currentCompany && !state.profile.bio && (
              <Notice tone="ink">Employment and biography details are added by the alumni office once you share them.</Notice>
            )}
          </>
        )}
      </div>
    </AppShell>
  );
}
