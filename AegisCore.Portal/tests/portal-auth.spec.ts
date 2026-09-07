import { test, expect } from "@playwright/test";
import { NextRequest } from "next/server";
import { POST, GET } from "../src/app/api/[...path]/route";

test.describe("Portal authentication boundary", () => {
  test("rejects cross-origin login before contacting the API", async () => {
    const response = await POST(new NextRequest("https://university.example/api/auth/login", { method: "POST", headers: { origin: "https://attacker.example" } }));
    expect(response.status).toBe(403);
  });
  test("stores tokens in secure HTTP-only cookies without returning them to JavaScript", async () => {
    const original = globalThis.fetch;
    globalThis.fetch = async () => Response.json({ accessToken: "test-access", refreshToken: "test-refresh", expiresIn: 900 });
    try {
      const response = await POST(new NextRequest("https://university.example/api/auth/login", { method: "POST", headers: { origin: "https://university.example", "content-type": "application/json" }, body: "{}" }));
      expect(await response.json()).toEqual({ authenticated: true });
      expect(response.cookies.get("aegiscore_access")).toMatchObject({ value: "test-access", httpOnly: true, secure: true, sameSite: "lax" });
      expect(response.cookies.get("aegiscore_refresh")).toMatchObject({ httpOnly: true, secure: true });
    } finally { globalThis.fetch = original; }
  });
  test("forwards cookie authentication and preserves backend authorization failures", async () => {
    const original = globalThis.fetch;
    let authorization = "";
    globalThis.fetch = async (_input, init) => { authorization = new Headers(init?.headers).get("authorization") ?? ""; return Response.json({ error: "forbidden" }, { status: 403 }); };
    try {
      const response = await GET(new NextRequest("https://university.example/api/students", { headers: { cookie: "aegiscore_access=test-access" } }));
      expect(authorization).toBe("Bearer test-access"); expect(response.status).toBe(403);
      expect(await response.json()).toEqual({ error: "forbidden" });
    } finally { globalThis.fetch = original; }
  });
});
