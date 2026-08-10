"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Eye, EyeOff, GraduationCap, Landmark, Scale, ShieldCheck, TrendingUp, UserRound, Users } from "lucide-react";
import { isApiError } from "@/lib/api-client";
import { isDemoMode, isDemoOfflinePreview, login, writeDemoSession, type DemoAccount } from "@/lib/session";
import { useSession } from "@/components/session-provider";
import { Spinner } from "@/components/async/states";

const testimonials = [
  { quote: "Finally, an ERP that doesn't require a manual. Just works.", author: "Faculty Member", icon: Users },
  { quote: "I checked my fee status in 10 seconds. No kidding.", author: "Student — BS Computer Science", icon: GraduationCap },
  { quote: "The VC dashboard showed me things I never had visibility into.", author: "University Administrator", icon: TrendingUp },
];

type DemoCard = DemoAccount & { password: string; icon: typeof GraduationCap };

const demoAccounts: DemoCard[] = [
  { email: "student@iobm.demo", password: "GoldenDemo!2026", label: "Student", icon: GraduationCap, desc: "Ali Hassan" },
  { email: "faculty@iobm.demo", password: "GoldenDemo!2026", label: "Faculty", icon: UserRound, desc: "Dr. Imran Siddiqui" },
  { email: "finance@iobm.demo", password: "GoldenDemo!2026", label: "Finance", icon: Landmark, desc: "Khalid Hussain" },
  { email: "registrar@iobm.demo", password: "GoldenDemo!2026", label: "Registrar", icon: Scale, desc: "Hira Nawaz" },
  { email: "admissions@iobm.demo", password: "GoldenDemo!2026", label: "Admissions", icon: ShieldCheck, desc: "Sana Mirza" },
];

export default function LoginPage() {
  const [showPassword, setShowPassword] = useState(false);
  const [testimonialIdx, setTestimonialIdx] = useState(0);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const router = useRouter();
  const { status, reload } = useSession();

  useEffect(() => {
    const interval = setInterval(() => setTestimonialIdx(i => (i + 1) % testimonials.length), 5000);
    return () => clearInterval(interval);
  }, []);

  // Already signed in → leave the login page.
  useEffect(() => {
    if (status === "authenticated") router.replace("/");
  }, [status, router]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (busy) return;
    setError("");
    setBusy(true);
    try {
      // Real backend login: sets HttpOnly cookies; tokens stay out of JS.
      await login(email.trim(), password);
      await reload();
      router.replace("/");
    } catch (err) {
      if (isApiError(err)) {
        if (err.status === 401) setError("Invalid email or password.");
        else if (err.isRateLimited) setError("Too many attempts. Please wait and try again.");
        else setError(err.message);
      } else {
        setError("The portal cannot reach the authentication service.");
      }
      setBusy(false);
    }
  };

  // Demo cards authenticate against the real backend in demo deployments;
  // the localStorage demo identity is only an offline preview fallback.
  const fillDemo = async (acc: DemoCard) => {
    if (busy) return;
    setEmail(acc.email);
    setPassword(acc.password);
    setError("");
    setBusy(true);
    if (isDemoOfflinePreview) {
      writeDemoSession(acc);
      await reload();
      router.replace("/");
      return;
    }
    try {
      await login(acc.email, acc.password);
      await reload();
      router.replace("/");
    } catch (err) {
      setError(isApiError(err)
        ? err.status === 401 ? "Demo credentials were rejected by the authentication service." : err.message
        : "The portal cannot reach the authentication service.");
      setBusy(false);
    }
  };

  const T = testimonials[testimonialIdx]!;
  const TI = T.icon;

  return (
    <main className="login-page">
      {/* Left panel */}
      <aside className="login-visual">
        <div className="login-visual-content">
          <div className="login-branding">
            <img src="/aegiscore-logo.png" alt="AegisCore" style={{ width: 44, borderRadius: 10 }} />
            <div>
              <strong style={{ fontSize: 20, color: "#fff", lineHeight: 1.1 }}>AegisCore</strong>
              <small style={{ fontSize: 11, color: "#94A3B8", letterSpacing: ".08em", textTransform: "uppercase" }}>The intelligent university.</small>
            </div>
          </div>

          <div className="login-testimonial">
            <TI size={28} style={{ color: "#60A5FA", marginBottom: 16 }} />
            <p style={{ fontSize: 18, lineHeight: 1.6, color: "#E2E8F0", fontStyle: "italic" }}>
              &ldquo;{T.quote}&rdquo;
            </p>
            <span style={{ display: "block", marginTop: 12, color: "#94A3B8", fontSize: 13 }}>
              — {T.author}
            </span>
            <div className="testimonial-dots">
              {testimonials.map((_, i) => (
                <button key={i} className={i === testimonialIdx ? "active" : ""} onClick={() => setTestimonialIdx(i)} aria-label={`Testimonial ${i + 1}`} />
              ))}
            </div>
          </div>

          <div className="login-powered-by">
            <span>Powering</span>
            <strong>Institute of Business Management</strong>
          </div>
        </div>

        <div className="login-geometric">
          <div className="geo-shape shape-1" /><div className="geo-shape shape-2" /><div className="geo-shape shape-3" />
        </div>
      </aside>

      {/* Right panel */}
      <section className="login-form-panel">
        <div className="login-form-container">
          <div style={{ textAlign: "center", marginBottom: 32 }}>
            <img src="/aegiscore-logo.png" alt="AegisCore" style={{ width: 40, borderRadius: 8, marginBottom: 12 }} />
            <h1 style={{ fontSize: 28, margin: 0 }}>Welcome back</h1>
            <p style={{ color: "var(--muted)", marginTop: 4 }}>Sign in to your AegisCore portal</p>
          </div>

          <form className="login-form" onSubmit={handleSubmit}>
            <label className="field-label">
              University email
              <input type="email" placeholder="you@iobm.edu.pk" autoComplete="email" required value={email} onChange={e => setEmail(e.target.value)} disabled={busy} />
            </label>
            <label className="field-label">
              Password
              <div className="password-field">
                <input type={showPassword ? "text" : "password"} placeholder="Enter your password" autoComplete="current-password" required value={password} onChange={e => setPassword(e.target.value)} disabled={busy} />
                <button type="button" className="password-toggle" onClick={() => setShowPassword(v => !v)} aria-label="Toggle password visibility">
                  {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </div>
            </label>

            {error && <p className="login-error" role="alert">{error}</p>}
            <button type="submit" className="btn btn-primary btn-lg" style={{ width: "100%" }} disabled={busy}>
              {busy ? <Spinner size={16} /> : "Sign in"}
            </button>
          </form>

          {isDemoMode && (
            <div className="login-demo-section">
              <div className="divider-row"><span>Demo Access — IoBM Pilot</span></div>
              <div className="demo-role-grid">
                {demoAccounts.map(a => {
                  const CardIcon = a.icon;
                  return (
                    <button key={a.email} type="button" className="demo-role-card" onClick={() => void fillDemo(a)} disabled={busy}>
                      <CardIcon size={18} className="demo-role-icon" />
                      <div>
                        <strong>{a.label}</strong>
                        <small>{a.desc}</small>
                      </div>
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          <div className="login-footer">
            <span>AegisCore v2.0 · Secured by Apex Systems</span>
          </div>
        </div>
      </section>

      <style>{`
        .login-page{display:grid;grid-template-columns:55% 45%;min-height:100vh;background:var(--surface)}
        .login-visual{position:relative;display:flex;align-items:center;justify-content:center;background:linear-gradient(160deg,#0F172A 0%,#1E3A5F 60%,#1D4ED8 100%);overflow:hidden}
        .login-visual-content{position:relative;z-index:2;max-width:440px;padding:48px;display:flex;flex-direction:column;justify-content:center;min-height:100vh}
        .login-branding{display:flex;align-items:center;gap:12px;margin-bottom:auto}
        .login-testimonial{margin:auto 0;padding:28px 0}
        .testimonial-dots{display:flex;gap:8px;margin-top:20px}
        .testimonial-dots button{width:8px;height:8px;border:none;border-radius:50%;background:rgba(255,255,255,.2);cursor:pointer;transition:all .2s;padding:0}.testimonial-dots button.active{width:24px;border-radius:4px;background:var(--primary)}
        .login-powered-by{margin-top:auto;padding-top:24px;border-top:1px solid rgba(255,255,255,.1);font-size:12px;color:#94A3B8}.login-powered-by strong{display:block;color:#E2E8F0;margin-top:2px}
        .login-geometric{position:absolute;inset:0;z-index:1;pointer-events:none}
        .geo-shape{position:absolute;border-radius:50%;opacity:.04}
        .shape-1{width:400px;height:400px;background:var(--primary);top:-100px;right:-100px;animation:geo-float 20s ease-in-out infinite}
        .shape-2{width:300px;height:300px;background:#fff;bottom:-80px;left:10%;animation:geo-float 15s ease-in-out infinite reverse}
        .shape-3{width:200px;height:200px;background:var(--primary);top:40%;right:30%;animation:geo-float 25s ease-in-out infinite 5s}
        @keyframes geo-float{0%,100%{transform:translate(0,0) scale(1)}33%{transform:translate(30px,-30px) scale(1.1)}66%{transform:translate(-20px,20px) scale(.9)}}
        .login-form-panel{display:flex;align-items:center;justify-content:center;padding:40px 32px;background:var(--surface)}
        .login-form-container{width:100%;max-width:400px}
        .login-form{display:grid;gap:16px}
        .field-label{display:grid;gap:6px;font-size:12px;font-weight:500;color:var(--muted)}
        .field-label input{width:100%;min-height:48px;padding:0 14px;border:1px solid var(--border-strong);border-radius:10px;background:var(--surface);font-size:15px;color:var(--ink);transition:border-color .15s,box-shadow .15s}
        .field-label input:focus{border-color:var(--primary);box-shadow:0 0 0 3px var(--primary-soft);outline:none}
        .password-field{position:relative}
        .password-field input{padding-right:48px}
        .password-toggle{position:absolute;right:10px;top:50%;transform:translateY(-50%);border:none;background:transparent;cursor:pointer;color:var(--muted);min-height:0;padding:4px}
        .login-error{margin:0;padding:10px 12px;border:1px solid var(--danger-soft);border-radius:8px;background:var(--danger-soft);color:var(--danger);font-size:13px;text-align:center}
        .divider-row{display:flex;align-items:center;gap:12px;margin:20px 0 12px}.divider-row::before,.divider-row::after{content:"";flex:1;height:1px;background:var(--border)}.divider-row span{font-size:11px;color:var(--muted);text-transform:uppercase;letter-spacing:.05em;white-space:nowrap}
        .demo-role-grid{display:grid;grid-template-columns:repeat(2,1fr);gap:6px}
        .demo-role-card{display:flex;align-items:center;gap:8px;padding:8px 10px;border:1px solid var(--border);border-radius:8px;background:var(--surface-muted);cursor:pointer;transition:border-color .15s,background .15s;text-align:left}.demo-role-card:hover{background:var(--primary-soft);border-color:var(--primary)}.demo-role-card:disabled{opacity:.5;cursor:not-allowed}
        .demo-role-icon{color:var(--primary);flex-shrink:0}
        .demo-role-card strong{font-size:11px;display:block}.demo-role-card small{font-size:10px;color:var(--muted)}
        .login-footer{margin-top:24px;text-align:center;font-size:11px;color:var(--subtle)}
        @media(max-width:768px){.login-page{grid-template-columns:1fr}.login-visual{display:none}.login-form-panel{padding:32px 20px}}
      `}</style>
    </main>
  );
}
