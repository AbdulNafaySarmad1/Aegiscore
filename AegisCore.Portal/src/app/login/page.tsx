"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Eye, EyeOff, Users, TrendingUp, GraduationCap } from "lucide-react";
import { apiPost, setAccessToken, setRefreshToken } from "@/lib/api-client";

const testimonials = [
  { quote: "Finally, an ERP that doesn't require a manual. Just works.", author: "Faculty Member", icon: Users },
  { quote: "I checked my fee status in 10 seconds. No kidding.", author: "Student — BS Computer Science", icon: GraduationCap },
  { quote: "The VC dashboard showed me things I never had visibility into.", author: "University Administrator", icon: TrendingUp },
];

const demoAccounts = [
  { email: "student@iobm.demo", password: "Koenigsegg$One1", label: "Student", icon: "🎓", desc: "Ali Hassan" },
  { email: "faculty@iobm.demo", password: "Pagani$Huayra3", label: "Faculty", icon: "👨‍🏫", desc: "Dr. Imran" },
  { email: "finance@iobm.demo", password: "Porsche$GT3RS", label: "Finance", icon: "💰", desc: "Khalid Hussain" },
  { email: "registrar@iobm.demo", password: "Ferrari$Roma22", label: "Registrar", icon: "📋", desc: "Dr. Sana" },
  { email: "vc@iobm.demo", password: "Lambo$Aventador1", label: "Vice Chancellor", icon: "👑", desc: "Prof. Waseem" },
];

export default function LoginPage() {
  const [showPassword, setShowPassword] = useState(false);
  const [testimonialIdx, setTestimonialIdx] = useState(0);
  const isDemo = process.env.NEXT_PUBLIC_DEMO_MODE === "true";
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSubmitting(true);
    try {
      const result = await apiPost<{ accessToken: string; refreshToken: string }>("/api/auth/login", { email, password });
      setAccessToken(result.accessToken);
      setRefreshToken(result.refreshToken);
      router.replace("/");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed. Please check your credentials.");
    } finally {
      setSubmitting(false);
    }
  };

  const fillDemo = (acc: typeof demoAccounts[number]) => {
    setEmail(acc.email);
    setPassword(acc.password);
    setError("");
  };

  useEffect(() => {
    const interval = setInterval(() => setTestimonialIdx(i => (i + 1) % testimonials.length), 5000);
    return () => clearInterval(interval);
  }, []);

  const T = testimonials[testimonialIdx]!;
  const TI = T.icon;

  return (
    <main className="login-page">
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
            <p style={{ fontSize: 18, lineHeight: 1.6, color: "#E2E8F0", fontStyle: "italic" }}>&ldquo;{T.quote}&rdquo;</p>
            <span style={{ display: "block", marginTop: 12, color: "#94A3B8", fontSize: 13 }}>— {T.author}</span>
            <div className="testimonial-dots">
              {testimonials.map((_, i) => (
                <button key={i} className={i === testimonialIdx ? "active" : ""} onClick={() => setTestimonialIdx(i)} aria-label={`Testimonial ${i + 1}`} />
              ))}
            </div>
          </div>

          <div className="login-powered-by"><span>Powering</span><strong>Institute of Business Management</strong></div>
        </div>
        <div className="login-geometric"><div className="geo-shape shape-1" /><div className="geo-shape shape-2" /><div className="geo-shape shape-3" /></div>
      </aside>

      <section className="login-form-panel">
        <div className="login-form-container">
          <div style={{ textAlign: "center", marginBottom: 32 }}>
            <img src="/aegiscore-logo.png" alt="AegisCore" style={{ width: 40, borderRadius: 8, marginBottom: 12 }} />
            <h1 style={{ fontSize: 28, margin: 0 }}>Welcome back</h1>
            <p style={{ color: "var(--muted)", marginTop: 4 }}>Sign in to your AegisCore portal</p>
          </div>

          <form className="login-form" onSubmit={handleSubmit}>
            <label className="field-label">University email
              <input type="email" placeholder="you@iobm.edu.pk" autoComplete="email" required value={email} onChange={e => setEmail(e.target.value)} />
            </label>
            <label className="field-label">Password
              <div className="password-field">
                <input type={showPassword ? "text" : "password"} placeholder="Enter your password" autoComplete="current-password" required value={password} onChange={e => setPassword(e.target.value)} />
                <button type="button" className="password-toggle" onClick={() => setShowPassword(v => !v)} aria-label="Toggle password visibility">
                  {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </div>
            </label>

            <div className="login-options">
              <label className="checkbox-row"><input type="checkbox" /><span>Remember me</span></label>
              <span style={{ fontSize: 13, color: "var(--muted)" }}>Password reset is managed by your university.</span>
            </div>

            {error && <p style={{ color: "var(--danger)", fontSize: 13, textAlign: "center", margin: 0 }}>{error}</p>}
            <button type="submit" className="btn btn-primary btn-lg" style={{ width: "100%" }} disabled={submitting}>
              {submitting ? "Signing in..." : "Sign in"}
            </button>
          </form>

          {isDemo && (
            <div className="login-demo-section">
              <div className="divider-row"><span>Demo Access — IoBM Pilot</span></div>
              <div className="demo-role-grid">
                {demoAccounts.map(a => (
                  <button key={a.email} type="button" className="demo-role-card" onClick={() => fillDemo(a)} title={`Fill ${a.label} demo credentials`}>
                    <span className="demo-role-icon">{a.icon}</span>
                    <div><strong>{a.label}</strong><small>{a.desc}</small></div>
                  </button>
                ))}
              </div>
              <p style={{ margin: "8px 0 0", fontSize: 11, color: "var(--muted)", textAlign: "center" }}>Select a demo role, then press Sign in. Demo access uses the real API authentication flow.</p>
            </div>
          )}

          <div className="login-footer"><span>AegisCore v2.0 · Secured by Apex Systems</span></div>
        </div>
      </section>

      <style>{`
        .login-page{display:grid;grid-template-columns:55% 45%;min-height:100vh;background:var(--surface)}
        .login-visual{position:relative;display:flex;align-items:center;justify-content:center;background:linear-gradient(160deg,#0F172A 0%,#1E3A5F 60%,#1D4ED8 100%);overflow:hidden}
        .login-visual-content{position:relative;z-index:2;max-width:440px;padding:48px;display:flex;flex-direction:column;justify-content:center;min-height:100vh}
        .login-branding{display:flex;align-items:center;gap:12px;margin-bottom:auto}
        .login-testimonial{margin:auto 0;padding:28px 0}.testimonial-dots{display:flex;gap:8px;margin-top:20px}
        .testimonial-dots button{width:8px;height:8px;border:none;border-radius:50%;background:rgba(255,255,255,.2);cursor:pointer;transition:all .2s;padding:0}.testimonial-dots button.active{width:24px;border-radius:4px;background:var(--primary)}
        .login-powered-by{margin-top:auto;padding-top:24px;border-top:1px solid rgba(255,255,255,.1);font-size:12px;color:#94A3B8}.login-powered-by strong{display:block;color:#E2E8F0;margin-top:2px}
        .login-geometric{position:absolute;inset:0;z-index:1;pointer-events:none}.geo-shape{position:absolute;border-radius:50%;opacity:.04}
        .shape-1{width:400px;height:400px;background:var(--primary);top:-100px;right:-100px;animation:geo-float 20s ease-in-out infinite}.shape-2{width:300px;height:300px;background:#fff;bottom:-80px;left:10%;animation:geo-float 15s ease-in-out infinite reverse}.shape-3{width:200px;height:200px;background:var(--primary);top:40%;right:30%;animation:geo-float 25s ease-in-out infinite 5s}
        @keyframes geo-float{0%,100%{transform:translate(0,0) scale(1)}33%{transform:translate(30px,-30px) scale(1.1)}66%{transform:translate(-20px,20px) scale(.9)}}
        .login-form-panel{display:flex;align-items:center;justify-content:center;padding:40px 32px;background:var(--surface)}.login-form-container{width:100%;max-width:400px}.login-form{display:grid;gap:16px}
        .field-label{display:grid;gap:6px;font-size:12px;font-weight:500;color:var(--muted)}.field-label input{width:100%;min-height:48px;padding:0 14px;border:1px solid var(--border-strong);border-radius:10px;background:var(--surface);font-size:15px;color:var(--ink);transition:border-color .15s,box-shadow .15s}.field-label input:focus{border-color:var(--primary);box-shadow:0 0 0 3px var(--primary-soft);outline:none}
        .password-field{position:relative}.password-field input{padding-right:48px}.password-toggle{position:absolute;right:10px;top:50%;transform:translateY(-50%);border:none;background:transparent;cursor:pointer;color:var(--muted);min-height:0;padding:4px}
        .login-options{display:flex;justify-content:space-between;align-items:center;gap:12px}.checkbox-row{display:flex;align-items:center;gap:8px;font-size:13px;color:var(--ink);cursor:pointer}.checkbox-row input{min-height:0;width:16px;height:16px;accent-color:var(--primary)}
        .divider-row{display:flex;align-items:center;gap:12px;margin:20px 0 12px}.divider-row::before,.divider-row::after{content:"";flex:1;height:1px;background:var(--border)}.divider-row span{font-size:11px;color:var(--muted);text-transform:uppercase;letter-spacing:.05em;white-space:nowrap}
        .demo-role-grid{display:grid;grid-template-columns:repeat(2,1fr);gap:6px}.demo-role-card{display:flex;align-items:center;gap:8px;padding:8px 10px;border:1px solid var(--border);border-radius:8px;background:var(--surface-muted);cursor:pointer;transition:all .15s}.demo-role-card:hover{background:var(--primary-soft);border-color:var(--primary)}.demo-role-icon{font-size:20px}.demo-role-card strong{font-size:11px;display:block}.demo-role-card small{font-size:10px;color:var(--muted)}
        .login-footer{margin-top:24px;text-align:center;font-size:11px;color:var(--subtle)}@media(max-width:768px){.login-page{grid-template-columns:1fr}.login-visual{display:none}.login-form-panel{padding:32px 20px}}
      `}</style>
    </main>
  );
}
