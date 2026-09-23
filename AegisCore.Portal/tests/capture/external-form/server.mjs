// A stand-in for JotForm, used to test and record the external form integration without an
// account on the real service. It is a separate program on its own port: it serves a 77-question
// course and teacher evaluation, and when a respondent submits, it delivers the response to the
// configured webhook exactly the way JotForm's WebHooks integration does — an HTTP POST of
// multipart/form-data with formID, submissionID, formTitle, pretty and rawRequest (a JSON string
// keyed q<N>_<name>). It never talks to AegisCore's database; AegisCore sees only that POST.
//
//   FORM_WEBHOOK_URL=<the URL AegisCore issued, token included> node server.mjs
//
// Every page says plainly that this is a test form, not JotForm.

import { createServer } from "node:http";
import { randomInt } from "node:crypto";
import { QUESTIONS, FORM_ID, FORM_TITLE, CHOICES } from "./questions.mjs";

const port = Number(process.env.FORM_PORT ?? 8095);
const webhookUrl = process.env.FORM_WEBHOOK_URL ?? "";
const deliveries = [];

const esc = value => String(value).replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
const redact = url => url.replace(/token=[^&]+/, match => `token=${match.slice(6, 10)}…(hidden)`);

const style = `
  body { font-family: "Segoe UI", Arial, sans-serif; margin: 0; background: #eef1f6; color: #0a1551; }
  .banner { background: #fff4d6; border-bottom: 2px solid #e0b94a; padding: 8px 24px; font-size: 14px; }
  header { background: #0a1551; color: #fff; padding: 18px 24px; display: flex; gap: 16px; align-items: center; }
  header .logo { background: #ff6100; border-radius: 6px; padding: 6px 10px; font-weight: 700; }
  main { max-width: 860px; margin: 24px auto; background: #fff; border-radius: 8px; padding: 28px 36px; box-shadow: 0 2px 10px rgba(10,21,81,.08); }
  h1 { margin-top: 0; font-size: 26px; } h2 { font-size: 18px; border-bottom: 1px solid #dfe3ec; padding-bottom: 6px; margin-top: 32px; }
  .q { padding: 10px 0; border-bottom: 1px dashed #e6e9f0; } .q .label { font-weight: 600; display: block; margin-bottom: 6px; }
  .q .key { color: #8a93b2; font-family: Consolas, monospace; font-size: 12px; margin-left: 6px; font-weight: 400; }
  .choices { display: flex; flex-wrap: wrap; gap: 6px 14px; font-size: 14px; }
  select, textarea { font-size: 15px; padding: 6px; min-width: 320px; }
  button { background: #ff6100; color: #fff; border: 0; border-radius: 4px; font-size: 16px; padding: 12px 28px; cursor: pointer; margin-top: 24px; }
  table { border-collapse: collapse; width: 100%; font-size: 14px; } td, th { border-bottom: 1px solid #e6e9f0; padding: 6px 8px; text-align: left; }
  code { background: #f2f4f8; padding: 2px 5px; border-radius: 3px; word-break: break-all; }
  .ok { color: #1d7a3c; font-weight: 700; } .bad { color: #b3261e; font-weight: 700; }`;

function page(title, body) {
  return `<!doctype html><html lang="en"><head><meta charset="utf-8"><title>${esc(title)}</title><style>${style}</style></head><body>
<div class="banner" role="note"><strong>Test form — a local stand-in for JotForm.</strong> Separate program on port ${port}; it sends each response to AegisCore in JotForm's webhook format.</div>
<header><span class="logo">Test Forms</span><span>${esc(FORM_TITLE)}</span><span style="margin-left:auto;font-size:13px">Form ID ${FORM_ID}</span></header>
<main>${body}</main></body></html>`;
}

function formPage() {
  let section = "";
  const parts = [];
  for (const q of QUESTIONS) {
    if (q.section !== section) { section = q.section; parts.push(`<h2>${esc(section)}</h2>`); }
    const key = `q${q.n}_${q.name}`;
    let input;
    if (q.type === "course") input = `<select name="${key}" required aria-label="${esc(q.text)}"><option value="">Please select</option>${q.options.map(o => `<option>${esc(o)}</option>`).join("")}</select>`;
    else if (q.type === "semester") input = `<select name="${key}" aria-label="${esc(q.text)}">${q.options.map(o => `<option>${esc(o)}</option>`).join("")}</select>`;
    else if (q.type === "text") input = `<textarea name="${key}" rows="3" aria-label="${esc(q.text)}"></textarea>`;
    else input = `<div class="choices" role="radiogroup" aria-label="Question ${q.n}">${CHOICES.map(c => `<label><input type="radio" name="${key}" value="${esc(c)}"> ${esc(c)}</label>`).join("")}</div>`;
    parts.push(`<div class="q"><span class="label">${q.n}. ${esc(q.text)}<span class="key">${key}</span></span>${input}</div>`);
  }
  return page(FORM_TITLE, `<h1>${esc(FORM_TITLE)}</h1><p>${QUESTIONS.length} questions. Your answers are anonymous.</p>
<form method="post" action="/submit">${parts.join("")}<button type="submit">Submit</button></form>`);
}

async function readForm(request) {
  const chunks = [];
  for await (const chunk of request) chunks.push(chunk);
  return new URLSearchParams(Buffer.concat(chunks).toString("utf8"));
}

/** Delivers one response the way JotForm's webhook does. */
async function deliver(answers) {
  const submissionID = `${Date.now()}${randomInt(100000, 999999)}`.slice(0, 19);
  const raw = { slug: `submit/${FORM_ID}`, jsExecutionTracker: "build-date-1695000000000", submitSource: "form", buildDate: "1695000000000", ...answers, event_id: `${Date.now()}_${FORM_ID}_test`, timeToSubmit: "74" };
  const pretty = QUESTIONS.filter(q => answers[`q${q.n}_${q.name}`]).map(q => `${q.text}:${answers[`q${q.n}_${q.name}`]}`).join(", ");
  const body = new FormData();
  body.set("formID", FORM_ID);
  body.set("submissionID", submissionID);
  body.set("webhookURL", redact(webhookUrl));
  body.set("ip", "127.0.0.1");
  body.set("formTitle", FORM_TITLE);
  body.set("pretty", pretty);
  body.set("username", "test-forms");
  body.set("rawRequest", JSON.stringify(raw));
  body.set("type", "WEB");
  const started = Date.now();
  let status = 0, reply = "";
  try {
    const response = await fetch(webhookUrl, { method: "POST", body });
    status = response.status;
    reply = await response.text();
  } catch (error) { reply = String(error); }
  const record = { at: new Date().toISOString(), submissionID, status, reply: reply.slice(0, 400), ms: Date.now() - started };
  deliveries.unshift(record);
  return record;
}

const server = createServer(async (request, response) => {
  const url = new URL(request.url ?? "/", `http://localhost:${port}`);
  const send = (code, html, type = "text/html; charset=utf-8") => { response.writeHead(code, { "content-type": type }); response.end(html); };
  if (request.method === "GET" && url.pathname === "/") return send(200, formPage());
  if (request.method === "GET" && url.pathname === "/settings/webhooks") {
    return send(200, page("Webhook settings", `<h1>Settings → Integrations → WebHooks</h1>
<p>When someone submits this form, a POST is sent to:</p><p><code data-testid="configured-webhook">${esc(webhookUrl ? redact(webhookUrl) : "(not configured)")}</code></p>
<p>Format: <code>multipart/form-data</code> with <code>formID</code>, <code>submissionID</code>, <code>formTitle</code>, <code>pretty</code> and <code>rawRequest</code> — the fields JotForm sends.</p>
<h2>Recent deliveries</h2>${deliveries.length === 0 ? "<p>None yet.</p>" : `<table><thead><tr><th>Sent</th><th>Submission ID</th><th>HTTP status from AegisCore</th><th>Reply</th></tr></thead><tbody>${deliveries.slice(0, 10).map(d => `<tr><td>${esc(new Date(d.at).toLocaleTimeString())}</td><td><code>${esc(d.submissionID)}</code></td><td class="${d.status === 200 ? "ok" : "bad"}">${d.status || "failed"}</td><td><code>${esc(d.reply)}</code></td></tr>`).join("")}</tbody></table>`}`));
  }
  if (request.method === "GET" && url.pathname === "/deliveries.json") return send(200, JSON.stringify(deliveries), "application/json");
  if (request.method === "POST" && url.pathname === "/submit") {
    if (!webhookUrl) return send(500, page("Not configured", "<p>FORM_WEBHOOK_URL is not set.</p>"));
    const form = await readForm(request);
    const answers = {};
    for (const q of QUESTIONS) {
      const key = `q${q.n}_${q.name}`;
      const value = form.get(key);
      if (value !== null && value !== "") answers[key] = value;
    }
    const result = await deliver(answers);
    return send(200, page("Thank you", `<h1>Thank you!</h1><p>Your submission has been received.</p>
<p>Submission ID <code data-testid="submission-id">${esc(result.submissionID)}</code></p>
<p>Webhook delivered to AegisCore: <span class="${result.status === 200 ? "ok" : "bad"}" data-testid="webhook-status">HTTP ${result.status || "failed"}</span> in ${result.ms} ms</p>
<p><code data-testid="webhook-reply">${esc(result.reply)}</code></p><p><a href="/">Submit another response</a></p>`));
  }
  send(404, page("Not found", "<p>Not found.</p>"));
});

server.listen(port, "127.0.0.1", () => console.log(`Test form (JotForm stand-in) on http://127.0.0.1:${port} → ${webhookUrl ? redact(webhookUrl) : "no webhook configured"}`));
