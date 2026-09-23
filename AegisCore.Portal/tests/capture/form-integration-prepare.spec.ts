import { expect, test, type Page } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { env, login } from "./helpers";
// @ts-expect-error -- plain ES module shared with the test form server.
import { FORM_ID, MAPPING } from "./external-form/questions.mjs";

// Prepares the dedicated local pilot for the form-integration recording, through the same API
// calls the screens make: the institution's outcome structure (CLOs for CS101 and CS102, the BSCS
// programme outcomes and the CLO→PLO links), one JotForm-format form connection with its token,
// and the form's 77-question mapping loaded as a pasted question list. Nothing is written to the
// database directly. The issued webhook URL goes to the ignored pilot state folder for the test
// form server to read; it is not printed.

type Outcomes = {
  courses: { id: string; code: string; clos: { id: string; code: string }[] }[];
  programs: { id: string; name: string; outcomes: { id: string; code: string }[] }[];
  mappings: { courseLearningOutcomeId: string; programOutcomeId: string }[];
};

const CLOS: Record<string, [string, string][]> = {
  CS101: [
    ["CLO1", "Explain how a program is executed: variables, types, control flow and memory."],
    ["CLO2", "Write structured programs that solve computational problems using functions, loops and arrays."],
    ["CLO3", "Trace, test and debug programs to find and correct faults."],
    ["CLO4", "Work in a small team to design and document a programming project."],
  ],
  CS102: [
    ["CLO1", "Explain objects, classes, encapsulation, inheritance and polymorphism."],
    ["CLO2", "Design class hierarchies for a given problem."],
    ["CLO3", "Implement and test object-oriented programs using standard libraries."],
    ["CLO4", "Communicate a software design in writing and in a presentation."],
  ],
};
const PLOS: [string, string][] = [
  ["PLO1", "Computing knowledge: apply knowledge of computing fundamentals to solve problems."],
  ["PLO2", "Design and development: design and build software that meets stated needs."],
  ["PLO3", "Problem analysis: identify, analyse and trace faults in computing systems."],
  ["PLO4", "Communication and teamwork: work in teams and communicate technical ideas."],
];
// CLO → PLO, with the strength the programme's curriculum map gives each link.
const LINKS: [string, string, string, string][] = [
  ["CS101", "CLO1", "PLO1", "High"], ["CS101", "CLO2", "PLO2", "High"], ["CS101", "CLO3", "PLO3", "High"], ["CS101", "CLO4", "PLO4", "Medium"],
  ["CS102", "CLO1", "PLO1", "Medium"], ["CS102", "CLO2", "PLO2", "High"], ["CS102", "CLO3", "PLO3", "Medium"], ["CS102", "CLO4", "PLO4", "High"],
];

async function call<T>(page: Page, method: string, path: string, body?: unknown): Promise<{ status: number; data: T; text: string }> {
  return await page.evaluate(async ({ method, path, body }) => {
    const response = await fetch(path, { method, headers: body === undefined ? {} : { "Content-Type": "application/json" }, body: body === undefined ? undefined : JSON.stringify(body) });
    const text = await response.text();
    let data: unknown = null; try { data = JSON.parse(text); } catch { /* text body */ }
    return { status: response.status, data: data as T, text };
  }, { method, path, body });
}

test("prepare the pilot for the form-integration recording", async ({ page }) => {
  test.setTimeout(180_000);
  expect(env("PILOT_CAPTURE_ACK")).toBe("dedicated-demo");
  expect(["localhost", "127.0.0.1"]).toContain(new URL(env("APP_URL")).hostname);
  await login(page, "admin");

  let outcomes = (await call<Outcomes>(page, "GET", "/api/obe/v2/outcomes")).data;
  if (!outcomes.courses.some(c => c.code === "CS101")) {
    const seed = await call(page, "POST", "/api/admin/demo-seed", {});
    expect(seed.status, `demo seed: ${seed.text}`).toBe(200);
    outcomes = (await call<Outcomes>(page, "GET", "/api/obe/v2/outcomes")).data;
  }

  for (const [code, clos] of Object.entries(CLOS)) {
    const course = outcomes.courses.find(c => c.code === code);
    expect(course, `course ${code}`).toBeTruthy();
    for (const [clo, description] of clos) {
      if (course!.clos.some(c => c.code === clo)) continue;
      const created = await call(page, "POST", `/api/obe/v2/courses/${course!.id}/clos`, { code: clo, description, bloomsLevel: "Apply" });
      expect(created.status, `${code} ${clo}: ${created.text}`).toBe(200);
    }
  }
  const program = outcomes.programs.find(p => /Computer Science/i.test(p.name)) ?? outcomes.programs[0];
  expect(program, "a degree programme").toBeTruthy();
  for (const [code, description] of PLOS) {
    if (program.outcomes.some(o => o.code === code)) continue;
    const created = await call(page, "POST", `/api/obe/v2/programs/${program.id}/outcomes`, { code, description });
    expect(created.status, `${code}: ${created.text}`).toBe(200);
  }
  outcomes = (await call<Outcomes>(page, "GET", "/api/obe/v2/outcomes")).data;
  const bscs = outcomes.programs.find(p => p.id === program.id)!;
  for (const [courseCode, cloCode, ploCode, strength] of LINKS) {
    const clo = outcomes.courses.find(c => c.code === courseCode)!.clos.find(c => c.code === cloCode)!;
    const plo = bscs.outcomes.find(o => o.code === ploCode)!;
    if (outcomes.mappings.some(m => m.courseLearningOutcomeId === clo.id && m.programOutcomeId === plo.id)) continue;
    const linked = await call(page, "POST", "/api/obe/v2/mappings", { cloId: clo.id, poId: plo.id, strength });
    expect(linked.status, `${courseCode} ${cloCode} → ${ploCode}: ${linked.text}`).toBe(200);
  }

  // One JotForm-format connection. A rerun issues a fresh token for the existing one.
  const sources = await call<{ id: string; name: string }[]>(page, "GET", "/api/integrations/forms/sources");
  expect(sources.status, sources.text).toBe(200);
  const name = "Course & Teacher Evaluation — Fall 2026";
  let sourceId = sources.data.find(s => s.name === name)?.id;
  let issued: { token: string; webhookUrl: string };
  if (!sourceId) {
    const created = await call<{ id: string; token: string; webhookUrl: string }>(page, "POST", "/api/integrations/forms/sources",
      { name, provider: "jotform", externalFormId: FORM_ID, scaleMax: 5, defaultSemester: "Fall 2026" });
    expect(created.status, created.text).toBe(200);
    sourceId = created.data.id; issued = created.data;
  } else {
    const rotated = await call<{ token: string; webhookUrl: string }>(page, "POST", `/api/integrations/forms/sources/${sourceId}/token`);
    expect(rotated.status, rotated.text).toBe(200);
    issued = rotated.data;
  }

  // The 77 questions, exactly as the mapping screen's "load the question list" sends them.
  const rows = (MAPPING as string).split("\n").map((line, index) => {
    const cells = line.split(",").map(c => c.trim());
    const target = cells[2];
    return {
      externalFieldId: cells[0], questionText: cells[1], target, sortOrder: index,
      cloCode: target === "Clo" ? cells[3] : null,
      category: target === "Satisfaction" ? cells[3] : null,
      programOutcomeId: null,
    };
  });
  expect(rows).toHaveLength(77);
  const saved = await call<{ saved: number; unmapped: number }>(page, "PUT", `/api/integrations/forms/sources/${sourceId}/mappings`, rows);
  expect(saved.status, saved.text).toBe(200);
  expect(saved.data).toMatchObject({ saved: 77, unmapped: 0 });

  const target = resolve(process.cwd(), "../.local/pilot/form-webhook.json");
  mkdirSync(dirname(target), { recursive: true });
  writeFileSync(target, `${JSON.stringify({ sourceId, webhookUrl: `${issued.webhookUrl}?token=${encodeURIComponent(issued.token)}` }, null, 2)}\n`, { encoding: "utf8", mode: 0o600 });
});
