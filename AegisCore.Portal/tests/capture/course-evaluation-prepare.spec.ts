import { expect, test, type Page } from "@playwright/test";
import { env, login } from "./helpers";

// Readies the dedicated local pilot for the course-evaluation recording, through the API only:
// removes the earlier JotForm test connection and the answers it recorded (the university is moving
// its surveys into AegisCore, so the recording starts from an empty report), and closes any survey
// left open by an earlier take. The CLOs, PLOs and CLO→PLO links were set up by
// form-integration-prepare and are reused as they are.

async function call<T>(page: Page, method: string, path: string, body?: unknown) {
  return await page.evaluate(async ({ method, path, body }) => {
    const response = await fetch(path, { method, headers: body === undefined ? {} : { "Content-Type": "application/json" }, body: body === undefined ? undefined : JSON.stringify(body) });
    const text = await response.text();
    let data: unknown = null; try { data = JSON.parse(text); } catch { /* not JSON */ }
    return { status: response.status, data: data as T, text };
  }, { method, path, body });
}

test("prepare the pilot for the course-evaluation recording", async ({ page }) => {
  expect(env("PILOT_CAPTURE_ACK")).toBe("dedicated-demo");
  expect(["localhost", "127.0.0.1"]).toContain(new URL(env("APP_URL")).hostname);
  await login(page, "admin");

  const sources = await call<{ id: string }[]>(page, "GET", "/api/integrations/forms/sources");
  expect(sources.status, sources.text).toBe(200);
  for (const source of sources.data) {
    const removed = await call(page, "DELETE", `/api/integrations/forms/sources/${source.id}?discardResponses=true`);
    expect(removed.status, removed.text).toBe(200);
  }
  const surveys = await call<{ id: string; status: string }[]>(page, "GET", "/api/obe/v2/surveys");
  expect(surveys.status, surveys.text).toBe(200);
  // Earlier takes of this recording leave a survey behind; close and delete them so the take starts clean.
  for (const survey of surveys.data) {
    if (survey.status === "Open") {
      const closed = await call(page, "POST", `/api/obe/v2/surveys/${survey.id}/close`);
      expect(closed.status, closed.text).toBe(200);
    }
    const deleted = await call(page, "DELETE", `/api/obe/v2/surveys/${survey.id}`);
    expect(deleted.status, deleted.text).toBe(200);
  }

  const outcomes = await call<{ courses: { code: string; clos: { code: string }[] }[] }>(page, "GET", "/api/obe/v2/outcomes");
  for (const code of ["CS101", "CS102"])
    expect(outcomes.data.courses.find(c => c.code === code)?.clos.map(c => c.code), `${code} CLOs`).toEqual(["CLO1", "CLO2", "CLO3", "CLO4"]);
  const report = await call<{ channels: { channel: string }[] }>(page, "GET", "/api/obe/v2/indirect");
  expect(report.data.channels.filter(c => c.channel.startsWith("external:")), "no JotForm test answers remain").toEqual([]);
});
