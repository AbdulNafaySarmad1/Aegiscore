import { NextRequest, NextResponse } from "next/server";

// Dedicated proxy for the public applicant portal (/apply, /terminal).
// It carries its OWN cookie (aegiscore_applicant) so an applicant session can never
// be confused with a staff/student session even at the browser layer.
export const dynamic = "force-dynamic";

const applicantCookie = "aegiscore_applicant";

async function proxy(request: NextRequest) {
  const path = request.nextUrl.pathname; // /api/applicant/...
  const mutating = !["GET", "HEAD", "OPTIONS"].includes(request.method);
  const origin = request.headers.get("origin");
  const expectedOrigin = new URL(process.env.APP_URL ?? request.url).origin;
  if (mutating && origin !== expectedOrigin) {
    return NextResponse.json({ error: "Invalid request origin" }, { status: 403 });
  }

  const headers = new Headers();
  for (const name of ["content-type", "accept"]) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }
  const token = request.cookies.get(applicantCookie)?.value;
  if (token) headers.set("authorization", `Bearer ${token}`);

  const body = mutating ? await request.arrayBuffer() : undefined;

  try {
    const upstream = await fetch(
      `${process.env.AEGISCORE_API_INTERNAL_URL ?? "http://api:8080"}${path}${request.nextUrl.search}`,
      { method: request.method, headers, body, cache: "no-store", redirect: "manual", signal: AbortSignal.timeout(60_000) }
    );

    const isVerify = path === "/api/applicant/auth/verify";
    if (isVerify && upstream.ok) {
      const payload = await upstream.json();
      if (typeof payload?.token !== "string") {
        return NextResponse.json({ error: "Invalid verification response" }, { status: 502 });
      }
      const response = NextResponse.json({ applicantId: payload.applicantId, status: payload.status });
      response.cookies.set(applicantCookie, payload.token, {
        httpOnly: true,
        secure: expectedOrigin.startsWith("https:"),
        sameSite: "lax",
        path: "/",
        maxAge: 60 * 60
      });
      response.headers.set("Cache-Control", "no-store");
      return response;
    }

    const response = new NextResponse(upstream.body, { status: upstream.status });
    for (const name of ["content-type", "content-disposition"]) {
      const value = upstream.headers.get(name);
      if (value) response.headers.set(name, value);
    }
    response.headers.set("Cache-Control", "no-store");
    if (upstream.status === 401) response.cookies.delete(applicantCookie);
    return response;
  } catch {
    return NextResponse.json({ error: "The applicant portal is temporarily unavailable. Please retry." }, { status: 502 });
  }
}

export { proxy as GET, proxy as POST, proxy as PUT, proxy as PATCH, proxy as DELETE, proxy as HEAD, proxy as OPTIONS };
