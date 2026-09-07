"use client";

import { useCallback, useEffect, useMemo, useState } from "react";

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
      setError(e instanceof Error ? e.message : "Something went wrong.");
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
      <style>{`
        .apply-shell{min-height:100vh;background:var(--bg,#f5f6f8);padding:32px 16px;display:flex;justify-content:center}
        .apply-card{width:100%;max-width:720px}
        .apply-stepper{display:flex;flex-wrap:wrap;gap:8px;margin-bottom:20px;font-size:12px}
        .apply-stepper span{padding:4px 10px;border-radius:999px;background:var(--surface,#fff);border:1px solid var(--border,#e2e4e9)}
        .apply-stepper span.active{background:var(--primary,#17233f);color:#fff;border-color:transparent}
        .apply-stepper span.done{opacity:.6}
        .apply-row{display:grid;gap:12px;margin:12px 0}
        .apply-actions{display:flex;justify-content:space-between;margin-top:20px;gap:12px}
        .apply-doc{display:flex;align-items:center;gap:12px;justify-content:space-between;border:1px solid var(--border,#e2e4e9);border-radius:8px;padding:12px;margin:8px 0}
        .apply-appid{font-size:22px;font-weight:700;letter-spacing:1px;padding:12px 16px;background:var(--primary-soft,#eef1f6);border-radius:8px;text-align:center;margin:16px 0}
      `}</style>
      <div className="apply-card">
        <h1 style={{ marginBottom: 4 }}>Apply to the University</h1>
        <p style={{ color: "var(--muted,#667)", marginTop: 0 }}>No account needed — sign up with your personal email.</p>

        <div className="apply-stepper">
          {STEPS.map((s, i) => (
            <span key={s} className={i === step ? "active" : i < step ? "done" : ""}>{i + 1}. {s}</span>
          ))}
        </div>

        {error && <div className="error-state" style={{ marginBottom: 12 }}>{error}</div>}

        <div className="card">
          <div className="card-body">
            {step === 0 && (
              <>
                <h3>Choose your intake and program</h3>
                <div className="apply-row">
                  <label>Intake
                    <select value={intakeId} onChange={e => { setIntakeId(e.target.value); setProgramId(""); }} style={{ width: "100%" }}>
                      <option value="">Select an intake…</option>
                      {intakes.map(i => <option key={i.intakeId} value={i.intakeId}>{i.name} ({i.year}{i.semester})</option>)}
                    </select>
                  </label>
                  <label>Program
                    <select value={programId} onChange={e => setProgramId(e.target.value)} disabled={!selectedIntake} style={{ width: "100%" }}>
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
                <h3>Verify your email</h3>
                <div className="apply-row">
                  <label>Personal email (Gmail, Outlook, any provider)
                    <input type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="you@example.com" style={{ width: "100%" }} />
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
                        <input inputMode="numeric" maxLength={6} value={otp} onChange={e => setOtp(e.target.value.replace(/\D/g, ""))} style={{ width: "100%" }} />
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
                <h3>Your details</h3>
                <div className="apply-row">
                  <label>Full name<input value={details.fullName} onChange={e => setDetails({ ...details, fullName: e.target.value })} style={{ width: "100%" }} /></label>
                  <label>Date of birth<input type="date" value={details.dateOfBirth} onChange={e => setDetails({ ...details, dateOfBirth: e.target.value })} style={{ width: "100%" }} /></label>
                  <label>Gender
                    <select value={details.gender} onChange={e => setDetails({ ...details, gender: e.target.value })} style={{ width: "100%" }}>
                      <option value="">Select…</option><option>Male</option><option>Female</option><option>Other</option>
                    </select>
                  </label>
                  <label>Nationality<input value={details.nationality} onChange={e => setDetails({ ...details, nationality: e.target.value })} style={{ width: "100%" }} /></label>
                  <label>Phone<input value={details.phone} onChange={e => setDetails({ ...details, phone: e.target.value })} style={{ width: "100%" }} /></label>
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
                <h3>Review &amp; submit</h3>
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
                <h3>Application submitted 🎉</h3>
                <p>Save your Applicant ID — you will need it for admission tests and to check your status.</p>
                <div className="apply-appid">{profile.applicantId}</div>
                <p>We have also emailed it to <strong>{profile.email}</strong>.</p>
              </>
            )}
          </div>
        </div>
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
      <h3>Upload your documents</h3>
      <p style={{ color: "var(--muted,#667)" }}>Only the documents your program requires are shown.</p>
      {certs.map(d => (
        <div key={d.id} className="apply-doc">
          <div>
            <strong>{d.documentType.replace(/_/g, " ")}</strong>{d.isRequired ? " *" : " (optional)"}
            <div style={{ fontSize: 12, color: "var(--muted,#667)" }}>
              {d.acceptedFormat === "Pdf" ? "PDF" : "JPEG/PNG"} · max {d.maxFileSizeMb} MB
              {d.uploadStatus && <> · <span className={`badge badge-${d.uploadStatus === "Approved" ? "success" : d.uploadStatus === "Rejected" ? "danger" : "warning"}`}>{d.uploadStatus}</span></>}
              {d.rejectionReason && <> · {d.rejectionReason}</>}
            </div>
          </div>
          <label className="btn btn-secondary btn-sm" style={{ cursor: "pointer" }}>
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
      <h3>Passport-style photo</h3>
      <p style={{ color: "var(--muted,#667)" }}>
        Plain, unfiltered photo of your face. No Snapchat/beauty filters, cartoon effects, or AI-generated images.
      </p>
      <label className="btn btn-secondary" style={{ cursor: "pointer" }}>
        {photo?.uploadStatus ? "Replace photo" : "Upload photo"}
        <input hidden type="file" accept="image/jpeg,image/png"
          onChange={async e => { if (e.target.files?.[0]) { const r = await onUpload(e.target.files[0]); if (r) setResult(r); } }} />
      </label>
      {status && (
        <div className={`form-notice`} style={{ marginTop: 12 }}>
          <span className={`badge badge-${status === "Approved" ? "success" : status === "Rejected" ? "danger" : "warning"}`}>{status}</span>{" "}
          {status === "Approved" && "Photo accepted."}
          {status === "Rejected" && (result?.rejectionReason ?? photo?.rejectionReason ?? "Photo did not pass the filter check. Please upload a plain photo.")}
          {status === "ManualReview" && "The automated check is unavailable — an admissions officer will review your photo. You can continue."}
          {result?.confidence != null && <> ({Math.round(result.confidence * 100)}% confidence)</>}
        </div>
      )}
      <div className="apply-actions">
        <button className="btn btn-ghost" onClick={onBack}>Back</button>
        <button className="btn btn-primary" disabled={busy || !status || status === "Rejected"} onClick={onNext}>Continue</button>
      </div>
    </>
  );
}
