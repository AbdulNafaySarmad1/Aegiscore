# External form integration

Institutions often run course evaluations, teacher-satisfaction surveys and CLO/PLO
self-assessments in an outside form tool (JotForm, Google Forms, Typeform, Microsoft Forms)
instead of in AegisCore. This integration takes each response the tool delivers by webhook,
works out what every answer means using a per-form field mapping, and records the ratings where
the outcome-based education (OBE) report reads them. The report treats an external response and
a native AegisCore form response the same way.

This is the first **incoming integration point** in AegisCore. It follows the pattern a later
plugin or marketplace connector should also follow: a documented wire contract, a small adapter
for each vendor, and a mapping layer shared by every vendor (see [24-plugin-ecosystem](../24-plugin-ecosystem.md)).

```
form tool ──POST──▶ /api/integrations/forms/{tenantId}/webhook
                     │  token → FormIntegrationSource (one per form)
                     │  IFormPayloadAdapter (per vendor) → FormSubmissionEnvelope
                     ▼
              FormIngestionService ── FormFieldMapping (what each field means)
                     │  stores FormIntegrationSubmission (answers only, replayable)
                     ▼
              OutcomeSurveyResponse (one row per rated answer; channel = native | external:<tool>)
                     ▼
              IndirectAssessmentReportService → GET /api/obe/v2/indirect → /obe/survey-results
```

## The contract

### Endpoint and authentication

`POST {APP_URL}/api/integrations/forms/{tenantId}/webhook`

Each form connection (a *source*) has its own token, which is shown once when it is issued or
rotated. Only its SHA-256 is stored. The token can be presented in three ways, checked in this
order:

| Where | Use when |
|---|---|
| `Authorization: Bearer <token>` | The tool lets you set headers (Typeform, Zapier, Make, a relay you write). |
| `X-AegisCore-Form-Token: <token>` | As above, if the tool keeps `Authorization` for itself. |
| `?token=<token>` in the URL | The tool's webhook settings take only a URL. **JotForm is one of these.** Keep the URL private and use HTTPS, because the token is in the URL. |

**Responses:** `200` with `{ accepted, submissionId, status, duplicate, fields, recorded, unmapped }`.
`401` means the token is missing, wrong, or belongs to another institution. `400` means the payload
could not be read, or it came from a different form than the token was issued for. A redelivered
submission (same submission id) is acknowledged with `duplicate: true` and is not counted again.

The portal's API proxy normally refuses any POST that doesn't carry the portal's own `Origin`.
The form webhook path (and only that exact path shape) is exempt, because it comes from another
server. It is never given a browser session. See `AegisCore.Portal/src/app/api/[...path]/route.ts`.

### Payload: the generic contract (`provider: generic`)

`Content-Type: application/json`

```json
{
  "formId": "optional — refused if it differs from the form id the source was set up with",
  "submissionId": "required; unique per form; used to recognise retries",
  "answers": [
    { "fieldId": "q5", "label": "I can write programs that solve problems (CLO2)", "value": 4 },
    { "fieldId": "q6", "value": "Agree" }
  ]
}
```

`answers` may instead be an object: `{ "q5": 4, "q6": "Agree" }`. Nested objects and arrays are
flattened to `field[key]` (so each row of a Likert matrix becomes its own field). The limits are
500 answers per submission, 200 characters per field id and 2,000 per value.

**Ratings** are read from a number (`4`), a numbered option (`4 - Agree`), or, on a 5-point scale
only, a label: Strongly disagree … Strongly agree, Very poor … Excellent, Very dissatisfied …
Very satisfied. A form on any other scale must send numbers. A rating outside 1..scale is
reported on that submission, not recorded.

### Payload: JotForm (`provider: jotform`)

JotForm's WebHooks integration sends `multipart/form-data` with `formID`, `submissionID`,
`formTitle`, `pretty` and `rawRequest`. `rawRequest` is a JSON string whose question keys look
like `q12_instructorKnowledge`. The adapter keeps only keys of that shape and drops `slug`,
`event_id`, `timeToSubmit` and the rest. JotForm doesn't sign its webhooks and has no header
setting, so the token goes in the URL.

## Mapping (the IT Lead screen: `/admin/integrations/forms`)

Each field of a form is mapped to one of:

| Meaning | Effect |
|---|---|
| Course | The answer names the course (`CS101`, `CS101 - Programming Fundamentals`, or the exact title). A source can instead be fixed to one course. |
| Instructor | Optional. If no field is mapped here, the course's assigned instructor is used. |
| Semester | Optional. If no field is mapped here, the source's default semester is used. |
| Rates a CLO (with a code, e.g. `CLO2`) | Resolved against the course each response is about, so one question works for every course on the form. |
| Rates a PLO directly | For a question that asks about a programme outcome itself. |
| Teaching / satisfaction item (with a heading) | Reported per course and per instructor under that heading. |
| Ignore | Comments, names and anything else that isn't a rating. |
| Not mapped yet | The default for every newly seen field. |

A form's fields show up on their own. Every field in a delivery that the source has not seen before
is added as *not mapped yet*. For a large form, don't wait for that: paste the question list in
*Load the question list in one go* (`field id, question text, meaning, CLO code or heading`, one
per line, comma- or tab-separated, straight out of a spreadsheet). The screen also has search,
filters (unmapped, changed, by meaning), select-all-shown and bulk mapping. The mapping table
scrolls with a sticky header.

Every delivery is stored (answers only, never the vendor's full envelope). *Save and re-apply*
re-runs the current mapping over every stored submission. A form that went live before it was
fully mapped therefore loses nothing: its responses are *Held* (no course) or *Partial* (some
fields unmapped), and they are completed once the mapping is.

### Scale check: a 77-question form

This was tested with a 77-question course and teacher evaluation (2 context questions, 4 CLO
items, 69 teaching and course items, 1 overall item, 1 comment):

- The pasted question list loads all 77 mappings in one request. `PUT /mappings` takes up to 1,000.
- `A_77_question_form_is_discovered_bulk_mapped_and_fully_recorded` (AegisCore.Tests) covers
  discovery of 77 fields, bulk mapping and full recording.
- The screen renders all 77 rows at once, with no paging and no virtualisation. That is fine at
  77 and will be fine at a few hundred. A form with thousands of fields (unlikely for a survey)
  would want virtualisation.

## Access control: an open question

The mapping screen and the webhook tokens are gated on a new permission resource,
**`Integrations`** (Read/Write). Administrators get it automatically, and the role wizard can grant
it to an IT Lead role. The reasoning: issuing a token lets an outside system write into the
institution's records, and that is an IT decision.

**Still to decide:** what each question *means* (which CLO a question rates) is curriculum
knowledge, and day to day it probably belongs to the Registrar or the OBE/QEC office rather than
IT. One option is to split the permission: `Integrations.Write` for tokens and connections (IT),
and a curriculum permission such as `Grades.Write` for the field mapping (Registrar). Revisit this
with the institution before go-live.

The report (`/obe/survey-results`, `GET /api/obe/v2/indirect`) uses the existing OBE permission,
`Grades.Read`.

## Onboarding a new form tool

1. If the tool (or a relay such as Zapier or Make) can post the generic JSON contract, nothing needs
   building. Create a source with `provider: generic`.
2. Otherwise, add an `IFormPayloadAdapter` that turns the tool's body into a
   `FormSubmissionEnvelope`, register it in `InfrastructureServiceExtensions`, and add its name to
   the providers list in `FormIntegrationsController`. The mapping, storage, replay and report are
   shared. `JotFormPayloadAdapter` is the example to copy (about 25 lines).
3. Add adapter tests beside the JotForm ones in `AegisCore.Tests/FormIntegrationTests.cs`.

## Going live with a real JotForm form

1. AegisCore must be reachable from the internet over **HTTPS**, because JotForm's servers post to
   it. A local or intranet-only installation needs a reverse proxy or tunnel for this path only.
2. Create the connection in *Form connections*: JotForm, the form's ID (from its URL), scale 5.
   Copy the webhook URL shown once.
3. In JotForm, go to *Settings → Integrations → WebHooks*, paste the URL and save.
4. Load the question list. JotForm's *Form Builder → question → Properties → Advanced → Field
   details* shows each question's unique name, and the key is `q<order>_<uniqueName>`. The
   quickest way to get the exact keys is one test submission: every key arrives as an unmapped
   field.
5. Map, then *Save and re-apply*. Delete the test submission's answers if they shouldn't count.
6. JotForm retries only a few times and has no dead-letter queue. For a survey that matters,
   schedule a reconciliation: compare JotForm's submission list (its API) with *Responses received*.
   That job is not built yet.

## Known limits

- The migration exists for PostgreSQL only, as with the other recent migrations. The MySQL, SQL
  Server and Oracle migration projects stop at September 2026.
- No reconciliation job pulls missed submissions from JotForm's API yet (see above).
- The token is carried in the URL for JotForm. That is the vendor's limitation, but it means the
  URL must be treated as a secret, and access logs in front of AegisCore should not record query
  strings for this path.
