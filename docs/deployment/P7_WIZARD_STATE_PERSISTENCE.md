# P7 — Wizard state persistence

The setup wizard is now stateless at the service boundary. `WizardStateService` loads and saves a `WizardStateDto` through the existing `WizardSession` table instead of retaining mutable singleton state.

## Persistence model

- A setup token is SHA-256 hashed before lookup; the raw bootstrap token is never stored in the database.
- `WizardSession.Payload` contains an ASP.NET Core Data Protection ciphertext of the complete wizard DTO.
- Credential values are written through the existing `ISecretService`; the protected payload contains only `secret://` references for those fields.
- Legacy `LicenseKey` and `StepData` columns are cleared when the encrypted payload is saved.
- Migration `PersistWizardState` adds only `BootstrapTokenHash`, `Payload`, and the unique token-hash index. No new table is introduced.

## Tenant identity and resume behavior

Step 1 calls `InitializeTenantAtStepOneAsync`. It creates a tenant ID only when the persisted session has no tenant ID and saves it immediately. Later saves preserve that value and reject attempts to replace it.

`ResumeWizardAsync(token)` reloads the encrypted state and its secret values. The API maps missing and completed sessions as follows:

- `404`: `Setup session not found. Start a new installation.`
- `409`: `This installation is already complete.`

## Verification

`WizardStatePersistenceTests.ResumeWizardAsync_RestoresStepsAndTenantAcrossFreshContainer` completes and persists Steps 1–3, disposes the first dependency-injection container, creates a new container, and verifies that the complete state and tenant ID are unchanged. It also verifies that the raw token and credential values are absent from the database row.
