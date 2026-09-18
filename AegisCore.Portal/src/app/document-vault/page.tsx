"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost, ApiError } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark, Notice } from "@/components/register";
import { Ledger } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Tier = { id: string; name: string; level: number; retentionYears: number; documentCount: number; totalSizeBytes: number };
type VaultDoc = {
  id: string; fileName: string; category: string; tierLevel: number; tierName: string; fileSizeBytes: number;
  isPqcVerified: boolean; pqcAlgorithm: string | null; isRetentionLocked: boolean;
  uploadedAt: string; retentionExpiresAt: string; yearsRemaining: number;
};
type Dashboard = { totalDocuments: number; totalSizeBytes: number; pqcVerifiedCount: number; lockedCount: number; tiers: Tier[]; recentUploads: VaultDoc[] };

/** Categories decide the tier, and the tier decides how long the document is kept. */
const CATEGORIES: { value: string; label: string; years: number }[] = [
  { value: "Degree", label: "Degree", years: 50 },
  { value: "Certification", label: "Certification", years: 50 },
  { value: "FinancialBilling", label: "Financial billing", years: 50 },
  { value: "FeeChallan", label: "Fee challan", years: 20 },
  { value: "GradeAssessment", label: "Grade assessment", years: 20 },
  { value: "Quiz", label: "Quiz", years: 20 },
  { value: "AcademicNotice", label: "Academic notice", years: 20 },
  { value: "Attendance", label: "Attendance", years: 5 },
  { value: "ProfilePicture", label: "Profile picture", years: 5 },
  { value: "General", label: "General", years: 5 },
];

function bytes(n: number): string {
  if (n < 1024) return `${n} B`;
  if (n < 1048576) return `${(n / 1024).toFixed(1)} KB`;
  if (n < 1073741824) return `${(n / 1048576).toFixed(1)} MB`;
  return `${(n / 1073741824).toFixed(2)} GB`;
}

/**
 * The document vault: what is kept, for how long, and whether the stored copy is still the one
 * that was signed. Verification re-reads the bytes, so it answers a real question.
 */
export default function DocumentVaultPage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Dashboard>(signal => apiGet<Dashboard>("/api/document-vault/dashboard", signal));
  const action = useAction();
  const [file, setFile] = useState<File | null>(null);
  const [category, setCategory] = useState("General");

  async function upload(e: FormEvent) {
    e.preventDefault();
    if (!file) return;
    await action.run("upload", async () => {
      const query = new URLSearchParams({
        fileName: file.name,
        contentType: file.type || "application/octet-stream",
        category,
        uploadedByUserId: "00000000-0000-0000-0000-000000000000",
        uploadedByRole: "Admin",
      });
      const response = await fetch(`/api/document-vault/upload?${query}`, { method: "POST", body: file, credentials: "include" });
      const body = await response.json().catch(() => ({}));
      if (!response.ok) throw new ApiError(body.error ?? `The upload did not complete (${response.status}).`, response.status);
      setFile(null);
      return `${file.name} is in tier ${body.tier}, locked for ${body.retentionYears} years, signed over content hash ${body.sha512}…`;
    }, reload);
  }

  async function verify(doc: VaultDoc) {
    await action.run(`verify-${doc.id}`, async () => {
      const result = await apiPost<{ verified: boolean }>(`/api/document-vault/verify/${doc.id}?verifiedByUserId=00000000-0000-0000-0000-000000000000`);
      return result.verified
        ? `${doc.fileName} still matches the hash recorded when it was vaulted, and its signature verifies.`
        : `${doc.fileName} no longer matches what was vaulted. It has been withheld from download — restore it from a backup.`;
    }, reload);
  }

  async function seedTiers() {
    await action.run("tiers", async () => {
      await apiPost("/api/document-vault/seed-tiers");
      return "The three retention tiers and their category rules are in place.";
    }, reload);
  }

  return (
    <ModulePage
      title="Document vault"
      crumbs={[{ label: "Records" }, { label: "Document vault" }]}
      meta={<span>Documents held under a retention lock, each signed over its content hash so a later reading can tell whether the stored copy is still the one that was filed.</span>}
      ident={(d: Dashboard) => <><strong>{d.totalDocuments}</strong><br />{d.totalDocuments === 1 ? "document" : "documents"}</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {dash => (
        <div className="record">
          <div className="record-body">
            <Figures aria-label="Vault figures" items={[
              { label: "Documents held", value: dash.totalDocuments, note: bytes(dash.totalSizeBytes) + " in all" },
              { label: "Signature verified", value: dash.pqcVerifiedCount, tone: dash.pqcVerifiedCount === dash.totalDocuments ? "laurel" : "oxblood", note: dash.pqcVerifiedCount === dash.totalDocuments ? "every document" : `${dash.totalDocuments - dash.pqcVerifiedCount} withheld` },
              { label: "Under retention lock", value: dash.lockedCount, note: "cannot be deleted before their term" },
              { label: "Longest term", value: `${Math.max(0, ...dash.tiers.map(t => t.retentionYears))} yr`, note: "tier 1, degrees and financial records" },
            ]} />

            <Panel title="Retention tiers" aside={<span className="text-muted">a category decides the tier</span>}>
              <Ledger rows={dash.tiers} keyOf={t => t.id} minWidth={640}
                empty={{
                  title: "No tiers defined yet",
                  detail: "Until the tiers exist there is nothing to file a document against.",
                  action: <button type="button" className={`btn btn-primary${action.busy === "tiers" ? " btn-loading" : ""}`} onClick={seedTiers} disabled={action.busy !== null}>Create the standard tiers</button>,
                }}
                columns={[
                  { key: "level", label: "Tier", render: t => `Tier ${t.level}`, nowrap: true },
                  { key: "name", label: "Holds", render: t => t.name },
                  { key: "years", label: "Kept for", render: t => `${t.retentionYears} years`, num: true, nowrap: true },
                  { key: "count", label: "Documents", render: t => t.documentCount, num: true },
                  { key: "size", label: "Size", render: t => bytes(t.totalSizeBytes), num: true, nowrap: true },
                ]} />
            </Panel>

            <Panel title="Recently filed" aside={<span className="text-muted">most recent first</span>} flush>
              <Ledger rows={dash.recentUploads} keyOf={d => d.id} minWidth={900}
                status={d => d.isPqcVerified ? undefined : "oxblood"}
                empty={{ title: "Nothing filed yet", detail: "A document filed here is stored, signed over its content hash and locked for its tier's term." }}
                columns={[
                  { key: "file", label: "Document", render: d => d.fileName },
                  { key: "category", label: "Category", render: d => d.category },
                  { key: "tier", label: "Tier", render: d => <>Tier {d.tierLevel}<br /><span className="text-muted">{d.tierName}</span></>, nowrap: true },
                  { key: "size", label: "Size", render: d => bytes(d.fileSizeBytes), num: true, nowrap: true },
                  { key: "filed", label: "Filed", render: d => date(d.uploadedAt), nowrap: true },
                  { key: "kept", label: "Kept until", render: d => <>{date(d.retentionExpiresAt)}<br /><span className="text-muted">{d.yearsRemaining} yr remaining</span></>, nowrap: true },
                  {
                    key: "signature", label: "Signature", render: d => d.isPqcVerified
                      ? <><Mark tone="laurel">Verified</Mark><br /><span className="text-muted">{d.pqcAlgorithm ?? "signed"}</span></>
                      : <><Mark tone="oxblood">Withheld</Mark><br /><span className="text-muted">does not match</span></>,
                  },
                  {
                    key: "act", label: "", render: d => (
                      <button type="button" className={`btn btn-ghost btn-sm${action.busy === `verify-${d.id}` ? " btn-loading" : ""}`} onClick={() => verify(d)} disabled={action.busy !== null}>
                        Check
                      </button>
                    ), nowrap: true,
                  },
                ]} />
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="File a document">
              <form className="panel-form" onSubmit={upload}>
                <label><span>Document</span>
                  <input type="file" onChange={e => setFile(e.target.files?.[0] ?? null)} required />
                </label>
                <label><span>Category</span>
                  <select value={category} onChange={e => setCategory(e.target.value)}>
                    {CATEGORIES.map(c => <option key={c.value} value={c.value}>{c.label} — {c.years} years</option>)}
                  </select>
                </label>
                <p className="field-hint">
                  The category sets the retention term, and the term cannot be shortened afterwards.
                  {file && <> This file is {bytes(file.size)}.</>}
                </p>
                <button type="submit" className={`btn btn-primary${action.busy === "upload" ? " btn-loading" : ""}`} disabled={action.busy !== null || !file}>
                  File and lock
                </button>
              </form>
            </Panel>

            <Panel title="What a check proves">
              <p>
                Checking a document re-reads the stored bytes, hashes them again, and verifies the
                signature this installation made when the document was filed. It answers one
                question: is this still the document that was filed?
              </p>
              <p>
                It is not a check that the document is genuine, correct, or the right document —
                only that nothing has altered it since. A document that fails is{" "}
                <Mark tone="oxblood">withheld</Mark> from download until someone restores it.
              </p>
            </Panel>

            {dash.tiers.length > 0 && dash.totalDocuments > dash.pqcVerifiedCount && (
              <Notice tone="oxblood" title="Some documents are withheld" role="alert">
                {dash.totalDocuments - dash.pqcVerifiedCount} stored {dash.totalDocuments - dash.pqcVerifiedCount === 1 ? "document does" : "documents do"} not match the hash recorded when
                they were filed. Restore them from a backup and check them again.
              </Notice>
            )}
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
