"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

const fadeIn = { animation: "loginFadeIn .6s ease both" };

export default function LoginPage() {
  const [background, setBackground] = useState<string | null>(null);
  const [clientLogo, setClientLogo] = useState<string | null>(null);

  useEffect(() => {
    fetch("/api/wizard/branding/background").then(r => r.json()).then(d => setBackground(d.url)).catch(() => {});
    fetch("/api/wizard/branding/logo").then(r => r.json()).then(d => setClientLogo(d.url)).catch(() => {});
  }, []);

  return (
    <>
      <style>{`
        @keyframes loginFadeIn {
          from { opacity: 0; transform: translateY(12px); }
          to { opacity: 1; transform: translateY(0); }
        }
        @keyframes loginPulse {
          0%, 100% { box-shadow: 0 8px 24px rgb(49 95 213 / 24%), 0 0 0 0 rgb(49 95 213 / 18%); }
          50% { box-shadow: 0 8px 24px rgb(49 95 213 / 24%), 0 0 0 12px rgb(49 95 213 / 0%); }
        }
        .login-overlay {
          position: fixed; inset: 0;
          background: linear-gradient(160deg, rgb(15 23 42 / .82) 0%, rgb(10 16 30 / .72) 100%);
          backdrop-filter: blur(2px);
          z-index: 0;
        }
        .login-content {
          position: relative; z-index: 1;
          display: flex; flex-direction: column;
          align-items: center; justify-content: center;
          min-height: 100vh; min-height: 100dvh;
          padding: clamp(20px, 5vw, 40px);
          text-align: center;
        }
        .login-brand { animation: loginFadeIn .5s ease both; }
        .login-panel {
          animation: loginFadeIn .7s ease .15s both;
          display: flex; flex-direction: column;
          align-items: center; gap: 1.25rem;
        }
        .login-btn {
          display: inline-flex; align-items: center; gap: 8px;
          padding: .8rem 2.2rem;
          border: 1px solid var(--blue-500, #4777e8);
          border-radius: 10px;
          background: linear-gradient(145deg, #4777e8, #315fd5);
          color: #fff !important;
          font-weight: 700; font-size: .95rem;
          text-decoration: none;
          cursor: pointer;
          animation: loginPulse 2.4s ease infinite;
          transition: transform .2s ease, filter .2s ease, box-shadow .2s ease;
        }
        .login-btn:hover { transform: translateY(-1px); filter: brightness(1.08); box-shadow: 0 12px 30px rgb(49 95 213 / 32%); }
        .login-btn:active { transform: translateY(0); filter: brightness(.94); }
        @media (max-width: 480px) {
          .login-btn { padding: .7rem 1.6rem; font-size: .88rem; width: 100%; justify-content: center; }
          .login-panel { gap: 1rem; }
        }
      `}</style>
      <main
        style={{
          position: "fixed", inset: 0,
          ...(background ? {
            backgroundImage: `url(${background})`,
            backgroundSize: "cover",
            backgroundPosition: "center",
            backgroundRepeat: "no-repeat",
          } : {
            background: "linear-gradient(160deg, #192944 0%, #10182c 72%)",
          }),
        }}
      >
        <div className="login-overlay" />
        <div className="login-content">
          <div className="login-brand" style={fadeIn}>
            <img src="/aegiscore-logo.png" alt="AegisCore" style={{ height: 52, width: "auto", marginBottom: ".75rem", borderRadius: 8 }} />
            {clientLogo && (
              <>
                <span style={{ color: "rgba(255,255,255,.35)", fontSize: "1.5rem", margin: "0 .6rem", verticalAlign: "middle" }}>|</span>
                <img src={clientLogo} alt="University" style={{ height: 44, width: "auto", borderRadius: 8, verticalAlign: "middle" }} />
              </>
            )}
          </div>
          <div className="login-panel" style={fadeIn}>
            <h1 style={{ color: "#fff", fontSize: "clamp(1.4rem, 4vw, 1.75rem)", fontWeight: 700, margin: 0, lineHeight: 1.2 }}>Sign in</h1>
            <p style={{ color: "rgba(255,255,255,0.68)", margin: 0, fontSize: "clamp(.85rem, 2vw, .95rem)", maxWidth: 340 }}>University ERP powered by AegisCore</p>
            <Link className="login-btn" href="/">
              Continue to workspace
            </Link>
          </div>
        </div>
      </main>
    </>
  );
}
