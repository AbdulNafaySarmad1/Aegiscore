# PROMPT 08 — FACILITIES, CAMPUS OPERATIONS, DIGITAL TWIN

Read and obey `prompts/00_MASTER_BACKEND_ARCHITECT.md` first.

Act as principal facilities/digital-campus architect. Generate atomic DeepSeek V4 Pro orders under `docs/backend-orders/08-facilities/`. Do not implement production code.

## Core target

AegisCore must evolve from fragmented campus entities/timetable strings into a production-grade campus-space platform with one canonical physical identity and one authoritative occupancy/conflict model.

Verify current source before relying on prior audit claims.

## Canonical hierarchy

Target a coherent hierarchy such as:

`Tenant → Campus → Building → Floor → Space`

Space may represent classrooms, lecture halls, labs, meeting rooms, offices, auditoria, exam halls, library areas, common areas and workspaces where justified. Avoid one table per room type unless current source demands it.

Space identity must be stable and independent from drawing/SVG/PDF geometry.

## Architectural / civil drawing ingestion

This is a first-class requirement.

A university should be able to provide building drawings/naqshas as:

- vector PDF;
- scanned PDF;
- PNG/JPEG scan;
- SVG;
- future CAD adapters such as DXF/DWG if justified.

Design atomic orders for a secure asynchronous ingestion pipeline:

`Upload → validate → immutable original storage → classify → extract → normalize → draft → engineer review → approve/publish`

The extractor should propose, where reliably detectable:

- floor boundary;
- rooms/spaces;
- room numbers/labels;
- offices/classrooms/labs;
- corridors;
- doors/entrances;
- stairs;
- elevators;
- washrooms/common areas;
- geometry/polygons;
- confidence and unresolved warnings.

**AI extraction must never silently publish authoritative building geometry.** A civil/facilities engineer reviews, corrects, splits/merges/relabels and approves the draft.

Preserve immutable original file + checksum + uploader + timestamp + source/revision metadata + extraction engine/version.

## Floor-plan lifecycle

Generate orders for:

- FloorPlan;
- FloorPlanVersion;
- extraction job/state;
- draft geometry;
- engineer review;
- active/published version;
- rollback/archive;
- audit;
- optimistic concurrency for two editors;
- geometry validation;
- sanitized SVG/PDF/image handling.

Reservations/history must reference stable Space IDs, not SVG element IDs or polygon IDs. A room can change polygon across plan revisions without breaking its historical identity.

## Space inventory and capabilities

Audit/complete:

- SpaceType/status;
- capacity;
- accessibility;
- bookability;
- operational hours;
- extensible capabilities such as projector, smart board, VC, lab equipment, Wi-Fi, UPS etc.;
- temporary capability outage without necessarily blocking the whole room;
- office/desk/workspace assignment with effective dates.

## Single occupancy kernel

Do not permit Timetable, Exams, Meetings, Events and Maintenance to keep independent authoritative notions of room availability.

Design one canonical scheduled occupancy ledger keyed by Tenant + Space + time interval, while each domain retains its own business table/workflow.

Sources include:

- timetable/class;
- examination;
- meeting;
- event;
- maintenance block;
- administrative hold.

Actual occupancy telemetry/check-in remains separate from scheduled occupancy.

## Database-level conflict guarantee

Frontend/service prechecks are UX only. PostgreSQL must provide the final atomic protection against concurrent double booking. Evaluate `tstzrange`/`tsrange`, GiST and exclusion constraints in the actual EF/Npgsql version. Orders must specify SQLSTATE/error mapping and HTTP conflict behavior.

Also design conflict checks for faculty, section/cohort, attendee, capacity, capability and maintenance as appropriate.

## Timetable/exam migration

If current source still uses free-text room/venue/classroom identifiers, generate migration orders to canonical Space FKs with deterministic backfill/manual-resolution reporting. Do not preserve free-text as permanent truth.

## Meeting booking

Generate backend orders for availability search, reserve, modify, cancel, recurring reservations, approval rules, optional check-in/no-show release, participants, owner authorization and audit.

## Live room/floor board

Backend must expose read models for:

- what is happening in a room now;
- next activity;
- next free interval;
- capacity/expected count;
- capabilities/accessibility;
- maintenance state;
- scheduled vs actual occupancy.

Do not put frontend presentation logic in the backend.

## Maintenance

Wire maintenance tickets/work orders to temporal Space blocks/capability outages. A broken projector can disable a capability while keeping the room usable; an electrical/structural issue can block the Space. Existing future reservations must be identified for relocation/notification workflow.

## Privacy

Assigned workspace (e.g. Professor → Office A312) is not the same as real-time presence. Define separate visibility policies for directory location and live occupancy.

## Calendar adapters

External Outlook/Google/local calendar systems are adapters/mirrors. AegisCore occupancy remains canonical. External booking ingestion must still pass through the same conflict kernel.

## Analytics

Plan read models for utilization, underused rooms, peak hours, meeting demand, no-show rate, maintenance downtime and building/floor utilization without making expensive analytics synchronous request bottlenecks.

## Final chat response

Return only:

`ORDERS_CREATED`, `FACILITIES_CURRENT`, `DRAWING_INGESTION_TARGET`, `OCCUPANCY_KERNEL_TARGET`, `P0_GAPS`, `FIRST_ORDER`, `BLOCKERS`, `PRODUCTION_CODE_CHANGED:NO`.