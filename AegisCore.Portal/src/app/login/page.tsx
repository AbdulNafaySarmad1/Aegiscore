import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = { title: "Sign in" };

export default function LoginPage() {
  return (
    <main className="launch-screen">
      <h1>Sign in</h1>
      <p>Authentication is provided by the AegisCore API.</p>
      <Link className="primary-button" href="/">Continue to workspace</Link>
    </main>
  );
}
