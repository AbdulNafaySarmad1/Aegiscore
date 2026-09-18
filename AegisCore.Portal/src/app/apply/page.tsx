"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { RegisterHead, Panel, Notice, Mark } from "@/components/register";

type OpenProgram = { programId: string; name: string; code: string };
type OpenIntake = { intakeId: string; year: number; semester: string; name: string; programs: OpenProgram[] };
type DocRequirement = {
  id: string;
  documentType: string;
  isRequired: boolean;
  acceptedFormat: "Pdf" | "Image";
  maxFileSizeMb: number;
  uploadStatus: string | null;
  rejectionReason: string | null;
};
type ProfileView = {
  applicantId: string;
  email: string;
  status: string;
  fullName: string | null;
  dateOfBirth: string | null;
  gender: string | null;
  nationality: string | null;
  phone: string | null;
  documents: DocRequirement[];
};

const STEPS = ["Program", "Verify email", "Your details", "Documents", "Photo", "Review", "Done"] as const;

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    credentials: "include",
    ...init,
    headers: init?.body instanceof FormData ? init?.headers : { "Content-Type": "application/json", ...(init?.headers ?? {}) }
  });
  const body = res.headers.get("content-type")?.includes("json") ? await res.json() : await res.text();
  if (!res.ok) throw new Error((body as { error?: string })?.error ?? `Request failed (${res.status})`);
  return body as T;
}

export default function ApplyPage() {
  const [step, setStep] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [intakes, setIntakes] = useState<OpenIntake[]>([]);
  const [intakeId, setIntakeId] = useState("");
  const [programId, setProgramId] = useState("");

  const [email, setEmail] = useState("");
  const [otpSent, setOtpSent] = useState(false);
  const [otp, setOtp] = useState("");

  const [profile, setProfile] = useState<ProfileView | null>(null);
  const [details, setDetails] = useState({ fullName: "", dateOfBirth: "", gender: "", nationality: "", phone: "" });

  const run = useCallback(async <T,>(fn: () => Promise<T>): Promise<T | undefined> => {
    setBusy(true);
    setError(null);
    try {
      return await fn();
    } catch (e) {
      setError(e instanceof Error ? e.message : "That step did not complete. Check your connection and try again.");
      return undefined;
    } finally {
      setBusy(false);
    }
  }, []);

  useEffect(() => {
    api<OpenIntake[]>("/api/applicant/intakes/open").then(setIntakes).catch(() => setError("Could not load open intakes."));
  }, []);

  const selectedIntake = useMemo(() => intakes.find(i => i.intakeId === intakeId), [intakes, intakeId]);
  const refreshProfile = useCallback(async () => {
    const p = await api<ProfileView>("/api/applicant/profile");
    setProfile(p);
    setDetails(d => ({
      fullName: p.fullName ?? d.fullName,
      dateOfBirth: p.dateOfBirth ?? d.dateOfBirth,
      gender: p.gender ?? d.gender,
      nationality: p.nationality ?? d.nationality,
      phone: p.phone ?? d.phone
    }));
  }, []);

  return (
    <main className="apply-shell">
      <div className="apply-card">
        <RegisterHead
          title="Apply to the university"
          meta={<span>No account is needed: you sign up with your own email address, and the applicant number you are given at the end is what carries you through admission.</span>}
          ident={<><strong>Step {step + 1}</strong><br />of {STEPS.length}</>}
        />

        <ol className="apply-stepper" aria-label="Application steps">
          {STEPS.map((s, i) => (
            <li key={s} className={i === step ? "active" : i < step ? "done" : ""} aria-current={i === step ? "step" : undefined}>{i + 1}. {s}</li>
          ))}
        </ol>

        {error && <Notice tone="oxblood" role="alert">{error}</Notice>}

        <Panel title={STEPS[step]}>
          <div className="panel-form">
            {step === 0 && (
              <>
                <div className="apply-row">
                  <label>Intake
                    <select value={intakeId} onChange={e => { setIntakeId(e.target.value); setProgramId(""); }}>
                      <option value="">Select an intake…</option>
                      {intakes.map(i => <option key={i.intakeId} value={i.intakeId}>{i.name} ({i.year}{i.semester})</option>)}
                    </select>
                  </label>
                  <label>Program
                    <select value={programId} onChange={e => setProgramId(e.target.value)} disabled={!selectedIntake}>
                      <option value="">Select a program…</option>
                      {selectedIntake?.programs.map(p => <option key={p.programId} value={p.programId}>{p.name} — {p.code}</option>)}
                    </select>
                  </label>
                </div>
                {intakes.length === 0 && !busy && <p>No intakes are open for applications right now.</p>}
                <div className="apply-actions">
                  <span />
                  <button className="btn btn-primary" disabled={!intakeId || !programId} onClick={() => setStep(1)}>Continue</button>
                </div>
              </>
            )}

            {step === 1 && (
              <>
                <div className="apply-row">
                  <label>Personal email (Gmail, Outlook, any provider)
                    <input type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="you@example.com" />
                  </label>
                  {!otpSent ? (
                    <button className="btn btn-secondary" disabled={busy || !email}
                      onClick={() => run(async () => {
                        await api("/api/applicant/auth/signup", { method: "POST", body: JSON.stringify({ email, intakeId, programId }) });
                        setOtpSent(true);
                      })}>Send verification code</button>
                  ) : (
                    <>
                      <label>Enter the 6-digit code we emailed you
                        <input inputMode="numeric" maxLength={6} value={otp} onChange={e => setOtp(e.target.value.replace(/\D/g, ""))} />
                      </label>
                      <button className="btn btn-primary" disabled={busy || otp.length !== 6}
                        onClick={() => run(async () => {
                          await api("/api/applicant/auth/verify", { method: "POST", body: JSON.stringify({ email, otp, intakeId, programId }) });
                          await refreshProfile();
                          setStep(2);
                        })}>Verify &amp; continue</button>
                      <button className="btn btn-ghost btn-sm" disabled={busy}
                        onClick={() => run(async () => { await api("/api/applicant/auth/signup", { method: "POST", body: JSON.stringify({ email, intakeId, programId }) }); })}>
                        Resend code
                      </button>
                    </>
                  )}
                </div>
                <div className="apply-actions">
                  <button className="btn btn-ghost" onClick={() => setStep(0)}>Back</button>
                  <span />
                </div>
              </>
            )}

            {step === 2 && (
              <>
                <div className="apply-row">
                  <label>Full name<input value={details.fullName} onChange={e => setDetails({ ...details, fullName: e.target.value })} /></label>
                  <label>Date of birth<input type="date" value={details.dateOfBirth} onChange={e => setDetails({ ...details, dateOfBirth: e.target.value })} /></label>
                  <label>Gender
                    <select value={details.gender} onChange={e => setDetails({ ...details, gender: e.target.value })}>
                      <option value="">Select…</option><option>Male</option><option>Female</option><option>Other</option>
                    </select>
                  </label>
                  <label>Nationality<input value={details.nationality} onChange={e => setDetails({ ...details, nationality: e.target.value })} /></label>
                  <label>Phone<input value={details.phone} onChange={e => setDetails({ ...details, phone: e.target.value })} /></label>
                </div>
                <div className="apply-actions">
                  <button className="btn btn-ghost" onClick={() => setStep(1)}>Back</button>
                  <button className="btn btn-primary" disabled={busy}
                    onClick={() => run(async () => {
                      await api("/api/applicant/profile", { method: "PUT", body: JSON.stringify(details) });
                      await refreshProfile();
                      setStep(3);
                    })}>Save &amp; continue</button>
                </div>
              </>
            )}

            {step === 3 && profile && (
              <DocumentsStep
                profile={profile}
                busy={busy}
                onUpload={(type, file) => run(async () => {
                  const form = new FormData();
                  form.append("file", file);
                  await api(`/api/applicant/documents/${type}`, { method: "POST", body: form });
                  await refreshProfile();
                })}
                onBack={() => setStep(2)}
                onNext={() => setStep(4)}
              />
            )}

            {step === 4 && profile && (
              <PhotoStep
                profile={profile}
                busy={busy}
                onUpload={file => run(async () => {
                  const form = new FormData();
                  form.append("file", file);
                  const r = await api<{ validationStatus: string; rejectionReason: string | null; confidence: number | null }>("/api/applicant/photo", { method: "POST", body: form });
                  await refreshProfile();
                  return r;
                })}
                onBack={() => setStep(3)}
                onNext={() => setStep(5)}
              />
            )}

            {step === 5 && profile && (
              <>
                <div className="apply-row">
                  <div><strong>Applicant ID:</strong> {profile.applicantId}</div>
                  <div><strong>Name:</strong> {profile.fullName}</div>
                  <div><strong>Email:</strong> {profile.email}</div>
                  <div><strong>Documents:</strong> {profile.documents.filter(d => d.uploadStatus).length} / {profile.documents.length} uploaded</div>
                </div>
                <div className="apply-actions">
                  <button className="btn btn-ghost" onClick={() => setStep(4)}>Back</button>
                  <button className="btn btn-primary" disabled={busy}
                    onClick={() => run(async () => { await api("/api/applicant/submit", { method: "POST" }); await refreshProfile(); setStep(6); })}>
                    Submit application
                  </button>
                </div>
              </>
            )}

            {step === 6 && profile && (
              <>
                <p>
                  Your application is with the admissions office. Keep the applicant number below:
                  it identifies you at the admission test and whenever you ask after your application.
                </p>
                <div className="apply-appid">{profile.applicantId}</div>
                <p>It has also been sent to <strong>{profile.email}</strong>.</p>
              </>
            )}
          </div>
        </Panel>
      </div>
    </main>
  );
}

function DocumentsStep({ profile, busy, onUpload, onBack, onNext }: {
  profile: ProfileView; busy: boolean;
  onUpload: (type: string, file: File) => void;
  onBack: () => void; onNext: () => void;
}) {
  const certs = profile.documents.filter(d => d.documentType !== "Photo");
  const requiredMissing = certs.some(d => d.isRequired && (!d.uploadStatus || d.uploadStatus === "Rejected"));
  return (
    <>
      <p className="field-hint">Only the documents your programme requires are listed. One marked with an asterisk must be uploaded before you can go on.</p>
      {certs.map(d => (
        <div key={d.id} className="apply-doc">
          <div>
            <strong>{d.documentType.replace(/_/g, " ")}</strong>{d.isRequired ? " *" : " (optional)"}
            <div className="apply-doc-meta">
              {d.acceptedFormat === "Pdf" ? "PDF" : "JPEG/PNG"} · max {d.maxFileSizeMb} MB
              {d.uploadStatus && <> · <Mark tone={d.uploadStatus === "Approved" ? "laurel" : d.uploadStatus === "Rejected" ? "oxblood" : "brass"}>{d.uploadStatus}</Mark></>}
              {d.rejectionReason && <> · {d.rejectionReason}</>}
            </div>
          </div>
          <label className="btn btn-secondary btn-sm apply-file">
            {d.uploadStatus ? "Replace" : "Upload"}
            <input hidden type="file" accept={d.acceptedFormat === "Pdf" ? "application/pdf" : "image/jpeg,image/png"}
              onChange={e => e.target.files?.[0] && onUpload(d.documentType, e.target.files[0])} />
          </label>
        </div>
      ))}
      <div className="apply-actions">
        <button className="btn btn-ghost" onClick={onBack}>Back</button>
        <button className="btn btn-primary" disabled={busy || requiredMissing} onClick={onNext}>Continue</button>
      </div>
    </>
  );
}

function PhotoStep({ profile, busy, onUpload, onBack, onNext }: {
  profile: ProfileView; busy: boolean;
  onUpload: (file: File) => Promise<{ validationStatus: string; rejectionReason: string | null; confidence: number | null } | void>;
  onBack: () => void; onNext: () => void;
}) {
  const photo = profile.documents.find(d => d.documentType === "Photo");
  const [result, setResult] = useState<{ validationStatus: string; rejectionReason: string | null; confidence: number | null } | null>(null);
  const status = result?.validationStatus ?? photo?.uploadStatus ?? null;
  return (
    <>
      <p className="field-hint">
        A plain photograph of your face, as you would use on a passport. Filters, cartoon effects and
        generated images are rejected, because this photograph is what identifies you at the test centre.
      </p>
      <label className="btn btn-secondary apply-file">
        {photo?.uploadStatus ? "Replace photograph" : "Upload photograph"}
        <input hidden type="file" accept="image/jpeg,image/png"
          onChange={async e => { if (e.target.files?.[0]) { const r = await onUpload(e.target.files[0]); if (r) setResult(r); } }} />
      </label>
      {status && (
        <Notice tone={status === "Approved" ? "laurel" : status === "Rejected" ? "oxblood" : "brass"} role="status">
          {status === "Approved" && "Your photograph has been accepted."}
          {status === "Rejected" && (result?.rejectionReason ?? photo?.rejectionReason ?? "This photograph did not pass the check. Upload a plain, unedited photograph of your face.")}
          {status === "ManualReview" && "The automatic check could not run, so an admissions officer will look at your photograph. You can carry on in the meantime."}
        </Notice>
      )}
      <div className="apply-actions">
        <button className="btn btn-ghost" onClick={onBack}>Back</button>
        <button className="btn btn-primary" disabled={busy || !status || status === "Rejected"} onClick={onNext}>Continue</button>
      </div>
    </>
  );
}
