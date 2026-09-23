import { NextRequest, NextResponse } from "next/server";

export const dynamic = "force-dynamic";
const accessCookie = "aegiscore_access";
const refreshCookie = "aegiscore_refresh";
// Machine-to-machine deliveries from an outside form tool (JotForm and the like). They come from
// another server, so they carry no Origin and no session; the API authenticates each one by the
// form token it presents. Only this exact path shape is exempt from the browser origin check, and
// it is never given the caller's session cookies.
const FORM_WEBHOOK = /^\/api\/integrations\/forms\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\/webhook$/i;
// PayFast's server-to-server payment notification (IPN). Same rules: no Origin, no session, exact
// path only; the API authenticates it by PayFast's validation hash and re-reads the result from
// PayFast. PayFast puts the detail in the query string, by GET or POST.
const PAYFAST_IPN = /^\/api\/payments\/payfast\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\/ipn$/i;

async function proxy(request: NextRequest) {
  const path = request.nextUrl.pathname;
  const mutating = !["GET", "HEAD", "OPTIONS"].includes(request.method);
  const payfastIpn = (request.method === "GET" || request.method === "POST") && PAYFAST_IPN.test(path);
  const webhook = (request.method === "POST" && FORM_WEBHOOK.test(path)) || payfastIpn;
  const origin = request.headers.get("origin");
  const expectedOrigin = new URL(process.env.APP_URL ?? request.url).origin;
  if (mutating && !webhook && origin !== expectedOrigin) {
    return NextResponse.json({ error: "Invalid request origin" }, { status: 403 });
  }
  const headers = new Headers();
  for (const name of ["content-type", "accept", "x-setup-token", "x-csrf-token", "cf-turnstile-response", "x-session-binding"]) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }
  if (webhook) {
    for (const name of ["authorization", "x-aegiscore-form-token", "x-forwarded-for"]) {
      const value = request.headers.get(name);
      if (value) headers.set(name, value);
    }
    if (!headers.has("x-forwarded-for")) headers.set("x-forwarded-for", request.headers.get("x-real-ip") ?? "form-webhook");
    try {
      const upstream = await fetch(`${process.env.AEGISCORE_API_INTERNAL_URL ?? "http://api:8080"}${path}${request.nextUrl.search}`, {
        method: request.method, headers, body: request.method === "POST" ? await request.arrayBuffer() : undefined, cache: "no-store", redirect: "manual", signal: AbortSignal.timeout(60_000),
      });
      return new NextResponse(upstream.body, { status: upstream.status, headers: { "content-type": upstream.headers.get("content-type") ?? "application/json", "cache-control": "no-store" } });
    } catch {
      return NextResponse.json({ error: `The university API did not answer; ${payfastIpn ? "PayFast" : "the form tool"} should retry.` }, { status: 502 });
    }
  }
  // The API rate-limits per client address; behind this proxy it would otherwise see one address
  // for everyone. An ingress in front of us sets x-forwarded-for; when nothing does — the portal
  // reached directly — the signed-in session is the next best discriminator, because a constant
  // here puts the whole university in one bucket and one busy user rate-limits everybody.
  const session = request.cookies.get("__Host-session-id")?.value;
  headers.set("x-forwarded-for",
    request.headers.get("x-forwarded-for")
    ?? request.headers.get("x-real-ip")
    ?? (session ? `portal-${session.slice(0, 32)}` : "portal"));
  const access = request.cookies.get(accessCookie)?.value;
  if (access) headers.set("authorization", `Bearer ${access}`);
  const binding = request.cookies.get("__Host-session-id")?.value;
  if (binding) headers.set("cookie", `__Host-session-id=${encodeURIComponent(binding)}`);
  let body = mutating ? await request.arrayBuffer() : undefined;
  if (path === "/api/auth/refresh" || path === "/api/auth/logout") {
    headers.set("content-type", "application/json");
    body = new TextEncoder().encode(JSON.stringify({ refreshToken: request.cookies.get(refreshCookie)?.value ?? "" })).buffer;
  }
  try {
    const upstream = await fetch(`${process.env.AEGISCORE_API_INTERNAL_URL ?? "http://api:8080"}${path}${request.nextUrl.search}`, {
      method: request.method, headers, body, cache: "no-store", redirect: "manual",
      signal: AbortSignal.timeout(60_000),
    });
    // The three answers that carry tokens: a password login, a refresh, and the exchange of the
    // one-time code an identity provider's callback handed the browser. All three become cookies here.
    const authenticate = ["/api/auth/login", "/api/auth/refresh", "/api/auth/sso/exchange", "/api/auth/login/mfa", "/api/auth/mfa/confirm"].includes(path);
    if (authenticate && upstream.ok) {
      const tokens = await upstream.json();
      // A password login may answer with a second-factor challenge instead of tokens; the
      // challenge goes to the browser as it is, and the code comes back through /api/auth/login/mfa.
      if (path === "/api/auth/login" && tokens.mfaRequired === true) {
        const challenge = NextResponse.json({ mfaRequired: true, challengeToken: tokens.challengeToken, expiresIn: tokens.expiresIn });
        challenge.headers.set("Cache-Control", "no-store");
        return challenge;
      }
      if (typeof tokens.accessToken !== "string" || typeof tokens.refreshToken !== "string")
        return NextResponse.json({ error: "Invalid authentication response" }, { status: 502 });
      const response = NextResponse.json({ authenticated: true, enabled: tokens.enabled });
      const options = { httpOnly: true, secure: expectedOrigin.startsWith("https:"), sameSite: "lax" as const, path: "/" };
      response.cookies.set(accessCookie, tokens.accessToken, { ...options, maxAge: tokens.expiresIn ?? 900 });
      response.cookies.set(refreshCookie, tokens.refreshToken, { ...options, maxAge: 7 * 86400 });
      response.headers.set("Cache-Control", "no-store");
      return response;
    }
    const response = new NextResponse(upstream.body, { status: upstream.status });
    // An SSO start or callback answers with a redirect — to the identity provider, or back to the
    // login screen with a one-time code or a reason. The browser must follow it, so Location passes through.
    for (const name of ["content-type", "content-disposition", "retry-after", "location"]) {
      const value = upstream.headers.get(name);
      if (value) response.headers.set(name, value);
    }
    response.headers.set("Cache-Control", "no-store");
    if (path === "/api/auth/logout" || (authenticate && upstream.status === 401)) {
      response.cookies.delete(accessCookie);
      response.cookies.delete(refreshCookie);
    }
    return response;
  } catch {
    return NextResponse.json({ error: "The university API is unavailable. Please retry." }, { status: 502 });
  }
}

export { proxy as GET, proxy as POST, proxy as PUT, proxy as PATCH, proxy as DELETE, proxy as HEAD, proxy as OPTIONS };
