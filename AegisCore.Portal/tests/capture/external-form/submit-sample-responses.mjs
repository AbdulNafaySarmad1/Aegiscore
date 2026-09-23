// Submits a batch of earlier responses to the test form, through its own /submit endpoint — the
// same request a browser makes — so each one reaches AegisCore by the form server's real webhook
// delivery. Used before a recording so the report has a history for the live response to join.
// The answers come from a fixed-seed generator, so a rerun produces the same spread.
//
//   node submit-sample-responses.mjs [count=14]

import { QUESTIONS } from "./questions.mjs";

const form = process.env.FORM_URL ?? "http://127.0.0.1:8095";
const count = Number(process.argv[2] ?? 14);
let seed = 20260923;
const next = () => (seed = (seed * 1103515245 + 12345) % 2147483648) / 2147483648;
const CHOICES = ["Strongly Disagree", "Disagree", "Neutral", "Agree", "Strongly Agree"];
// Mostly positive, as course evaluations are, with a real tail.
const pick = bias => { const r = next() + bias; return CHOICES[r < 0.08 ? 0 : r < 0.18 ? 1 : r < 0.38 ? 2 : r < 0.78 ? 3 : 4]; };

let delivered = 0;
for (let i = 0; i < count; i++) {
  const course = i % 7 < 4 ? QUESTIONS[0].options[0] : QUESTIONS[0].options[1];
  const bias = course.startsWith("CS101") ? 0.05 : -0.05;
  const body = new URLSearchParams();
  for (const q of QUESTIONS) {
    const key = `q${q.n}_${q.name}`;
    if (q.type === "course") body.set(key, course);
    else if (q.type === "semester") body.set(key, q.options[0]);
    else if (q.type === "rating") body.set(key, pick(bias));
  }
  const response = await fetch(`${form}/submit`, { method: "POST", body });
  const html = await response.text();
  const ok = /data-testid="webhook-status">HTTP 200</.test(html);
  if (ok) delivered++;
  console.log(`${i + 1}/${count} ${course.slice(0, 5)} → ${ok ? "delivered (HTTP 200)" : "NOT delivered"}`);
}
if (delivered !== count) { console.error(`${count - delivered} response(s) were not delivered.`); process.exit(1); }
