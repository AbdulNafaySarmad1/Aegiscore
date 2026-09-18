import { NextRequest, NextResponse } from "next/server";

export const dynamic = "force-dynamic";

/**
 * The realtime hubs, reached through the portal.
 *
 * The browser cannot put an Authorization header on a hub connection, and the access token lives in
 * an http-only cookie the page's script cannot read, so a direct connection to the API was refused
 * with 401 every time — on every finance screen, in every deployment, filling the console with
 * negotiation failures. Here the cookie becomes a bearer token, exactly as it does for the API.
 *
 * This route carries SignalR's long-polling transport. A WebSocket cannot be upgraded through a
 * route handler; where an ingress proxies /hubs to the API directly (the shipped nginx does), the
 * API reads the same cookie itself and the socket transport works.
 */
async function proxy(request: NextRequest) {
  const upstream = process.env.AEGISCORE_API_INTERNAL_URL ?? "http://api:8080";
  const headers = new Headers();
  for (const name of ["content-type", "accept", "x-requested-with", "x-signalr-user-agent"]) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }
  const access = request.cookies.get("aegiscore_access")?.value;
  if (access) headers.set("authorization", `Bearer ${access}`);
  headers.set("x-forwarded-for", request.headers.get("x-forwarded-for") ?? request.headers.get("x-real-ip") ?? "portal");

  const mutating = !["GET", "HEAD", "OPTIONS"].includes(request.method);
  try {
    const response = await fetch(`${upstream}${request.nextUrl.pathname}${request.nextUrl.search}`, {
      method: request.method,
      headers,
      body: mutating ? await request.arrayBuffer() : undefined,
      cache: "no-store",
      redirect: "manual",
      // Long polling holds the request open until the server has something to say.
      signal: AbortSignal.timeout(120_000),
    });
    const proxied = new NextResponse(response.body, { status: response.status });
    const contentType = response.headers.get("content-type");
    if (contentType) proxied.headers.set("content-type", contentType);
    proxied.headers.set("Cache-Control", "no-store");
    return proxied;
  } catch {
    return NextResponse.json({ error: "The university API is unavailable. Please retry." }, { status: 502 });
  }
}

export { proxy as GET, proxy as POST, proxy as DELETE, proxy as OPTIONS };
