import { access } from "node:fs/promises";
export const dynamic = "force-dynamic";
export async function GET() {
  try { await access("/tmp/aegiscore-draining"); return new Response("Draining", { status: 503 }); } catch { /* serving */ }
  try {
    const response = await fetch(`${process.env.AEGISCORE_API_INTERNAL_URL ?? "http://api:8080"}/health/ready`, { cache: "no-store", signal: AbortSignal.timeout(4000) });
    return new Response(response.ok ? "Ready" : "Not ready", { status: response.ok ? 200 : 503 });
  } catch { return new Response("Not ready", { status: 503 }); }
}
