# PROMPT 04 — FINANCE, PAYMENTS, PROCUREMENT

Read and obey `prompts/00_MASTER_BACKEND_ARCHITECT.md` first.

Act as principal finance/accounting architect. Generate atomic DeepSeek V4 Pro orders under `docs/backend-orders/04-finance/`. Do not implement production code.

## Standard

Treat university finance as a compact but serious institutional finance/accounting platform — operationally deep, auditable and correct. Do not pretend the university is legally a bank and do not build irrelevant banking regulation features.

## Audit and complete where required

- chart of accounts;
- double-entry journals and balancing invariants;
- student account/fee ledger;
- invoices/challans;
- receipts;
- credits/debits;
- payment allocation;
- scholarships/financial aid/waivers;
- installment plans;
- late fees/penalties;
- refunds/reversals;
- cash-office workflows;
- bank accounts and reconciliation;
- payment gateways;
- Raast/1Bill/other provider adapters where applicable;
- webhook verification/idempotency/replay protection;
- maker-checker and approval limits;
- department budgets;
- encumbrances;
- purchase requests;
- purchase orders;
- vendors;
- procurement approvals;
- payables/receivables;
- expense claims;
- payroll integration;
- period close;
- trial balance;
- general ledger;
- income/expense reports;
- balance sheet/cash-flow where accounting model supports them;
- audit/PQC/receipt integrity;
- reversal rather than destructive editing for posted financial records.

## Re-verify known risks

Targeted source verification must include:

- any currently failing finance executive/dashboard query;
- synchronous report generation/double serialization;
- payment webhook replay/idempotency;
- caller-supplied actor identifiers;
- tenant scoping;
- approval ownership/limits;
- multi-replica scheduled finance workers;
- provider-specific payment coupling;
- integrity/signature verification flows.

## Invariants

Generated orders must preserve or introduce explicit invariants for:

- journal debits == credits;
- posted transactions are never silently rewritten;
- reversals reference originals;
- payment webhook duplicates cannot double-credit;
- invoice/payment allocation totals are consistent;
- money uses deterministic decimal/currency handling;
- actor/approver comes from authenticated identity;
- cross-tenant finance access is impossible;
- financial reports derive from canonical ledger state;
- PQC/QR/verification metadata is preserved where already part of product behavior.

## Performance/operability

Expensive report generation should be evaluated for async/off-request execution. Define indexes, pagination, batch operations and observability for high-cardinality finance tables. Worker orders must include failure behavior for bank/gateway outages and retry only where idempotency makes it safe.

## Provider abstraction

Payment providers and outbound finance integrations must sit behind explicit adapters; tenant configuration selects them. Do not hard-code one bank/provider into core accounting logic.

## Final chat response

Return only:

`ORDERS_CREATED`, `FINANCE_COMPLETENESS_CURRENT`, `ACCOUNTING_INVARIANTS`, `P0_GAPS`, `FIRST_ORDER`, `BLOCKERS`, `PRODUCTION_CODE_CHANGED:NO`.