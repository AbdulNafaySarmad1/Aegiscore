import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = { title: "Sign in — AegisCore" };

export default function LoginPage() {
  return (
    <main className="launch-screen">
      <img src="/aegiscore-logo.png" alt="AegisCore" style={{ height: 48, width: "auto", marginBottom: "1.5rem", borderRadius: 6 }} />
      <h1>Sign in</h1>
      <p>University ERP powered by AegisCore</p>
      <Link className="primary-button" href="/">Continue to workspace</Link>
    </main>
  );
}
