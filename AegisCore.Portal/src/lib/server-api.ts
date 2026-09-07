import "server-only";
import { cookies } from "next/headers";

export async function serverGet<T>(path: string): Promise<T> {
  const cookieStore = await cookies();
  const response = await fetch(`${process.env.AEGISCORE_API_INTERNAL_URL ?? "http://api:8080"}${path}`, {
    cache: "no-store",
    headers: { authorization: `Bearer ${cookieStore.get("aegiscore_access")?.value ?? ""}` }
  });
  if (!response.ok) throw new Error(`AegisCore API request failed (${response.status})`);
  return response.json() as Promise<T>;
}
