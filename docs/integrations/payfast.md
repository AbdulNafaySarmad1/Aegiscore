# PayFast (Pakistan) — fee payment by hosted checkout

Status, 23 September 2026: **built and tested against AegisCore's side of the contract; not yet run
against PayFast's sandbox**, because no PayFast UAT merchant credentials exist in this environment.
PayFast issues them only to a registered merchant. See [What is and is not verified](#what-is-and-is-not-verified).

This is PayFast **Pakistan** (gopayfast.com, operated by APPS; hosts `*.apps.net.pk`). It is a
different company from PayFast South Africa (payfast.co.za), and the two protocols are not compatible.

## What existed before this work

| Area | State found on 23 Sep 2026 |
|---|---|
| PayFast | Nothing. No code, config or reference anywhere. |
| RAAST | `RaastPaymentGatewayAdapter` posts to `https://api.raast.gov.pk/v1/rtp/requests`, which is not a documented public API (RAAST is reached through a participating bank). It has never been exercised for real. A second client, `Payments/Raast/RaastApiClient.cs`, is unused. |
| "Sandbox" gateways | `SandboxPaymentGatewayAdapter` returns a fake `SANDBOX-…` reference after an 800 ms delay and never calls anything. Any gateway configured as `sandbox` is routed to it. |
| Generic webhook `/api/webhooks/{tenant}/{gateway}` | Real: HMAC-SHA256 signature, ML-DSA-65 signature in production, Redis dedupe, serializable recording. It expects AegisCore's own JSON envelope, which no real gateway sends. |
| Fee ledger (`FeeDemand`) | Real: create demand, record a manual payment (`OPEN → PARTIAL → PAID`), waivers, scholarships, installments, payment plans, `OVERDUE` from the payment-plan sweep, GL posting of receipts. **No in-progress state**: starting a request-to-pay changed nothing on the demand. |
| **Bug found** | `RecordGatewayPaymentAsync` wrote a 64-character SHA-256 hex digest into `FinancePayments.IdempotencyKey`, which is `varchar(40)`. SQLite ignores the length limit and PostgreSQL rejects the insert, so **no gateway payment could ever have been recorded on PostgreSQL**. It is now fixed (the key is truncated to 40 hex characters, 160 bits). |
| Student fees navigation | "Fee Status" and "Pay Fee" linked to `/student/finance`, which had no page. There is now an index page that redirects to the student's own statement. |

## The flow

```
Student (portal)          AegisCore API                         PayFast (apps.net.pk)
  │ Pay by card ─────────▶ POST /api/payments/payfast/checkout
  │                         ├─ GetAccessToken(MERCHANT_ID, SECURED_KEY, BASKET_ID, TXNAMT) ──▶
  │                         ├─ demand → PAYMENT_PENDING, FeeCheckoutSession PENDING
  │ ◀── hidden order fields (no card fields) + PostTransaction URL
  │ browser form POST ───────────────────────────────────────────▶ hosted payment page
  │                                                   student enters card details on PayFast
  │                         ◀── IPN: GET/POST /api/payments/payfast/{tenant}/ipn?basket_id&err_code&validation_hash&transaction_id…
  │ ◀── redirect to SUCCESS_URL / FAILURE_URL (/student/finance/checkout?basket=…&<PayFast's params>)
  │ page POSTs the params ▶ /checkout/{basket}/confirm  (same verification as the IPN)
```

Both the IPN and the student's return go through `PayFastCheckoutService.ProcessNotificationAsync`,
and neither is trusted on its own:

1. **Validation hash.** `sha256("basket_id|secured_key|merchant_id|err_code")`, lowercase hex, compared
   in constant time. Only PayFast and the merchant hold the secured key, so a match authenticates
   the basket and the error code, **and nothing else**. The amount and the transaction ID are not covered.
2. **The basket must be one AegisCore issued**, for this tenant (and, on the return path, for the
   signed-in student).
3. **A success is read back from PayFast's transaction status API** (`:8443/api/token`, then
   `/api/transaction/{id}`). The record must name the same basket, carry a success code
   (`000`, `00` or `0`), and carry *exactly* the amount the attempt was opened for. Only then is the
   payment recorded. If the record names another basket, the notice is rejected: an attacker can
   name a cheap transaction of their own, but it will not point at a victim's basket. If the amount
   differs, the attempt goes to `NEEDS_REVIEW` rather than being applied.

Recording reuses `IFinanceModuleService.RecordGatewayPaymentAsync`, the same path the generic webhook
uses. It posts the GL receipt and is keyed on `payfast:{transaction_id}` (the unique index on
`WebhookId`), so a retried IPN, or an IPN racing the student's return, records the payment once.

The generic `/api/webhooks/{tenant}/payfast` route refuses `payfast` explicitly, so that route
cannot be used to bypass these checks.

### Why the return page also settles payments

PayFast cannot reach a localhost IPN, and in production an IPN can arrive late or not at all. The
return carries the same hash-authenticated parameters, so it gets the same scrutiny and the same
idempotent recording. A forged return settles nothing.

## Fee demand state machine (the resolved part of the fee-ledger question)

```
                ┌──────────── verified failure (err_code ≠ 00), student cancels, or 30 min expiry ─┐
                ▼                                                                                   │
  OPEN / PARTIAL / OVERDUE ── student starts PayFast checkout ──▶ PAYMENT_PENDING ─────────────────┤
                                                                     │                              │
                                                                     └─ verified success ──▶ PAID (full amount)
```

* The status before the attempt is stored on the attempt (`FeeCheckoutSession.PreviousDemandStatus`)
  and restored **exactly**: an `OVERDUE` challan goes back to `OVERDUE`, not to `OPEN`.
* A restore happens only while the demand is still `PAYMENT_PENDING` and no other attempt is live. If
  something else moved the demand on meanwhile (a manual receipt, a waiver), that is kept.
* `PAYMENT_PENDING` still counts as outstanding in every existing report (they all test `!= "PAID"`).

Attempt (`FeeCheckoutSessions`) states: `PENDING → PAID | FAILED | CANCELLED | EXPIRED | SUPERSEDED | NEEDS_REVIEW`.

* **SUPERSEDED**: the student started a newer attempt for the same challan.
* **NEEDS_REVIEW**: PayFast verifiably took money that AegisCore could not apply, either a
  different amount or a challan already settled another way. The demand is restored and nothing is
  applied; finance must resolve it by hand. **There is no refund flow** (see open questions).
* **A verified success is honoured from any non-PAID state.** PayFast took the money, so a late
  success on a cancelled, expired or superseded attempt is still recorded. If the challan is
  already paid by then, the attempt goes to `NEEDS_REVIEW`.
* `FeeCheckoutExpiryWorker` runs every 5 minutes and expires `PENDING` attempts past `ExpiresAt`
  (30 minutes, set by `PaymentGateways:payfast:CheckoutMinutes`). If an IPN already named a
  transaction but the status API was down, the sweep re-checks with PayFast before expiring.
  The return page also expires a stale attempt when it is viewed.

## PCI DSS scope

AegisCore has no card field anywhere. The browser posts a form of **order** fields (merchant ID,
merchant name, one-time token, basket, amount, currency, date, description, return and IPN URLs,
and optionally the student's mobile and email) to PayFast's page, and the card is typed there.
The action URL is checked client-side to be `https://*.apps.net.pk` before submission. This is the
redirect model that keeps the merchant in SAQ A territory. It does **not** remove all obligations:
under PCI DSS v4.0.1, requirements 6.4.3 and 11.6.1 (script inventory and tamper detection on the
page that redirects to the payment page) apply to SAQ A merchants. Those belong to the compliance
programme, not to this change.

## Secrets and logging

* `MERCHANT_ID`, `SECURED_KEY` and `MERCHANT_NAME` live in the tenant vault under
  `/tenants/{tenant}/gateways/payfast/…`, written through the existing `WizardGatewayService`
  (Finance → Payment gateways → PAYFAST → Configure). They are never in source or config files,
  and never returned by any API.
* PayFast is exempt from the "production gateways need an ML-DSA-65 webhook key" rule. PayFast
  cannot produce ML-DSA signatures, and its IPN is authenticated as described above instead.
  The exemption is a one-entry allowlist (`HashAuthenticatedGateways`), not a relaxation for
  other gateways.
* Logs carry basket ID, tenant, transaction ID, error code and outcome. They never carry the secured
  key, the access token, the validation hash, raw payloads or the student's contact details. The
  API's request log records the path only, not the query string where PayFast puts the IPN detail.
  `PaymentGatewayEvents.SanitizedPayloadJson` holds only basket, transaction, amount, code and channel.
  A test (`No_Secret_Or_Token_Reaches_The_Logs_Across_A_Whole_Payment`) captures every log line of
  a full payment plus a forged IPN and asserts none of those values appear.

## Configuration

| Key | Default | Meaning |
|---|---|---|
| `PaymentGateways:payfast:SandboxBaseUrl` | `https://ipguat.apps.net.pk` | UAT GetAccessToken / PostTransaction host |
| `PaymentGateways:payfast:LiveBaseUrl` | `https://ipg1.apps.net.pk` | Live host |
| `PaymentGateways:payfast:SandboxStatusBaseUrl` / `LiveStatusBaseUrl` | same hosts on `:8443` | Transaction status API |
| `PaymentGateways:payfast:CheckoutMinutes` | `30` | Attempt lifetime (5–240) |
| `PaymentGateways:payfast:PublicCallbackBaseUrl` | `APP_URL` | Origin PayFast calls for the IPN; the portal proxies `/api/payments/payfast/{tenant}/ipn` to the API as a machine-to-machine request, without cookies |
| `APP_URL` | — | Portal origin used for SUCCESS_URL / FAILURE_URL |
| Tenant setting `payment.default_gateway` | `stripe` | Set to `payfast` so the student's Pay button uses PayFast |

## What is and is not verified

Verified here (`AegisCore.Tests/PayFastCheckoutTests.cs`, 15 tests, run on both SQLite and
PostgreSQL 17; related finance, gateway and wizard suites: 105/105 on both):

* Starting a checkout calls GetAccessToken with the right basket and amount, marks the demand
  `PAYMENT_PENDING`, and returns a hosted form with no card fields.
* A verified IPN marks the demand `PAID` with PayFast's transaction ID stored on the payment.
* A replayed IPN plus the student's return records one payment.
* The return alone settles when the IPN never arrives.
* A forged hash changes nothing and does not even call PayFast.
* A transaction from another basket is rejected.
* A wrong amount goes to review, not paid.
* A verified failure restores the exact prior status and allows a retry.
* Cancel restores the demand, and a later verified payment is still honoured.
* Paying twice from two tabs applies one payment and flags the other `NEEDS_REVIEW`.
* Expiry restores the demand.
* If the status API is down, the IPN returns 503 and the sweep settles it later.
* Students are isolated from one another's attempts.
* No secret reaches the logs.
* The migration applies to a fresh PostgreSQL database.
* PayFast's UAT hosts answer at every endpoint used (`GetAccessToken`, `PostTransaction`, `:8443/api/token`).

**Not verified: needs PayFast UAT credentials**

1. That the hosted page shows the right amount and basket (prompt phase 2.3).
2. The exact field names PayFast's status API returns. The adapter accepts the variants PayFast's
   reference integrations read (`transaction_amount` / `purchase_amount` / `TXNAMT`, `status_code`,
   `basket_id`), but this must be confirmed against a real sandbox response.
3. Whether PayFast requires `SIGNATURE` or `CUSTOMER_EMAIL_ADDRESS` on PostTransaction. Neither is
   sent unless the email is on the student profile. PayFast's own Blesta integration does not send
   `SIGNATURE`.
4. Whether PayFast sends the IPN by GET or POST, and whether it retries on a non-200. Both methods
   are accepted.
5. The whole end-to-end pass in a browser (prompt phase 6), including cancelling on PayFast's page.

### Sandbox run, once credentials exist

1. Finance → Payment gateways → PAYFAST → Configure: `MERCHANT_ID`, `SECURED_KEY`, `MERCHANT_NAME`
   (as registered with PayFast), `ENV` = `sandbox`. Then Admin → Settings → Default payment gateway = `payfast`.
2. Make the IPN reachable: set `APP_URL` (or `PaymentGateways:payfast:PublicCallbackBaseUrl`) to a
   public HTTPS origin, for example a tunnel to the portal.
3. As a seeded student with a PKR challan: Fee Status → Pay by card with PayFast. Confirm PayFast's
   page shows the challan amount and basket `FD-…-XXXXXX`, and pay with a PayFast test card.
4. Confirm the return page shows *Payment received* with PayFast's reference, the statement shows the
   challan `PAID`, and `FeeCheckoutSessions` / `FinancePayments` hold the same transaction ID.
5. Repeat and cancel on PayFast's page. The return page should say *not completed* or let the
   student cancel, and the challan should go back to its previous status.
6. `grep` the API logs for the secured key and the access token; there should be no matches.

## Open fee-ledger questions (deliberately out of scope)

* **Installments.** A PayFast attempt always pays the *whole* outstanding amount. Paying a single
  installment, or a partial amount, is not offered.
* **Refunds and disputes.** There is no refund flow. `NEEDS_REVIEW` attempts, and any chargeback,
  are handled by hand in PayFast's merchant portal and then recorded manually.
* **Double payment.** If a student pays twice through two tabs, the second verified payment lands
  in `NEEDS_REVIEW` and is not applied. Refunding it is manual.
* **Currency.** PayFast attempts are PKR only; demands in other currencies are refused.
* **RAAST.** The existing adapter targets an endpoint that does not appear to exist; whether to keep,
  fix or remove it is a separate decision.
* **Other database providers.** The migration is PostgreSQL only, like the other recent feature
  migrations; the MsSql, MySql and Oracle migration projects were last updated at phase 22.
