import { NextRequest, NextResponse } from "next/server";

export const dynamic = "force-dynamic";

/**
 * The health endpoints live on the API, and the ones worth reading — /health/modules,
 * /health/schema — require a signed-in administrator. The portal keeps the session in a cookie and
 * the API wants a bearer, so a plain rewrite left every signed-in request answered 401: the report
 * all three guides point at could not be reached by the person they were written for. This forwards
 * the cookie as the bearer the API expects, exactly as the /api proxy does. The unauthenticated
 * probes (/health, /health/live, /health/ready) pass through unchanged.
 */
export async function GET(request: NextRequest) {
  const headers = new Headers();
  const accept = request.headers.get("accept");
  if (accept) headers.set("accept", accept);
  const access = request.cookies.get("aegiscore_access")?.value;
  if (access) headers.set("authorization", `Bearer ${access}`);
  headers.set("x-forwarded-for", request.headers.get("x-forwarded-for") ?? request.headers.get("x-real-ip") ?? "portal");
  try {
    const upstream = await fetch(`${process.env.AEGISCORE_API_INTERNAL_URL ?? "http://api:8080"}${request.nextUrl.pathname}${request.nextUrl.search}`, {
      headers, cache: "no-store", redirect: "manual", signal: AbortSignal.timeout(30_000),
    });
    const response = new NextResponse(upstream.body, { status: upstream.status });
    const contentType = upstream.headers.get("content-type");
    if (contentType) response.headers.set("content-type", contentType);
    response.headers.set("Cache-Control", "no-store");
    return response;
  } catch {
    return NextResponse.json({ status: "unavailable", summary: "The register did not answer. Check that the application container is running." }, { status: 502 });
  }
}
