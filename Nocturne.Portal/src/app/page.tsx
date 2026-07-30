import { PortalShell } from "@/components/portal-shell";
import { redirect } from "next/navigation";

export default async function HomePage() {
  try {
    const response = await fetch("http://api:8080/health/installation", { cache: "no-store" });
    if (response.ok && !(await response.json()).isInstalled) redirect("/setup/");
  } catch {
    // Keep the portal available in local development when the API is not running.
  }

  return <PortalShell />;
}
