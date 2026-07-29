# Nocturne — Onboarding Wizard Prompt

## Role
You are a senior .NET 10 / C# 14 full-stack engineer building the Nocturne first-run onboarding wizard. This wizard is the very first thing any university sees when they spin up Nocturne for the first time. It must be polished, bulletproof, fully dynamic, and work for ANY university — zero hardcoding of any institution-specific value anywhere.

## Context
- Stack: C# 14, .NET 10, Blazor Server (or Razor Pages — whichever is already in use in the project)
- Database: PostgreSQL (connection string provided by user during wizard)
- Cache: Redis (connection string provided by user during wizard)
- Storage: Abstracted via `IStorageProvider` — user selects provider during wizard
- Frontend hosting: Cloudflare Pages
- The wizard runs ONLY on first boot (no configuration file or `is_configured` DB flag exists yet)
- After wizard completion, normal login page is served on all subsequent boots

---

## Wizard Flow — 7 Steps

### Step 1 — Welcome Screen
- Display Nocturne logo and version number (pulled from assembly, not hardcoded)
- Heading: "Welcome to Nocturne — Let's get your university set up"
- Single CTA button: "Begin Setup"
- No university-specific content on this screen

### Step 2 — University Identity
Fields:
- **University Full Name** (text, required) — stored as `university_name` in config
- **University Short Name / Abbreviation** (text, required, max 10 chars) — used in subdomain and UI
- **University Logo** (file upload, PNG/SVG, max 2MB) — stored via `IStorageProvider`
- **Primary Colour** (hex colour picker) — used for UI theming
- **Secondary Colour** (hex colour picker)
- **University Website URL** (text, required, validated as URL) — e.g. `https://ssuet.edu.pk`

Validation: all fields required before proceeding.

### Step 3 — Database Configuration
Fields:
- **PostgreSQL Connection String** (textarea, required)
  - Option A: Raw input — user pastes connection string directly
  - Option B: Builder mode — individual fields (Host, Port, Database, Username, Password, SSL Mode) assembled into connection string automatically
  - Toggle between Option A and Option B with a "Switch to builder / Switch to raw" link
- **Test Connection** button — attempts actual DB connection before allowing next step
  - Success: green checkmark, "Connection successful"
  - Failure: red error, exact PostgreSQL error message shown, "Try again" button
- Do NOT proceed to Step 4 until connection test passes

After successful connection:
- Run all pending EF Core migrations automatically
- Show migration progress (list of migrations applied)
- If migration fails: show exact error, offer "View Logs" and "Retry" options

### Step 4 — Cache Configuration
Fields:
- **Redis Connection String** (textarea, required)
  - Option A: Raw input
  - Option B: Builder mode (Host, Port, Password, Database index)
  - Same toggle as Step 3
- **Test Connection** button — same success/failure pattern as Step 3
- Do NOT proceed until connection test passes

### Step 5 — Storage Configuration
Fields:
- **Storage Provider** (dropdown): Local Disk | MinIO (S3-compatible) | Amazon S3 | Azure Blob Storage | SFTP
- Based on selection, show relevant fields:
  - **Local Disk**: Base path (default `/app/storage`)
  - **MinIO / S3**: Endpoint URL, Bucket Name, Access Key, Secret Key, Region
  - **Azure Blob**: Connection String, Container Name
  - **SFTP**: Host, Port, Username, Password/Key, Base Path
- **Test Storage** button — attempts to write and read a small test file
- Do NOT proceed until storage test passes

### Step 6 — Subdomain & DNS Setup
Display:
- "Your Nocturne portal will be accessible at a subdomain of your university website."
- **Suggested subdomain prefix** (editable text input, default: `nocturne`)
  - Preview updates live: `nocturne.ssuet.edu.pk` (using university website from Step 2)
- **Generate DNS Record** button — produces the exact DNS record they need to add:

```
Type:  CNAME
Name:  nocturne          (or whatever prefix they chose)
Value: [Cloudflare Pages deployment URL]
TTL:   Auto
```

- Display this record in a clearly formatted box with a one-click copy button
- Instructions: "Log in to your domain registrar or DNS provider and add this CNAME record. DNS propagation typically takes 5–30 minutes."
- **Verify DNS** button — polls the DNS record every 10 seconds for up to 5 minutes
  - Success: "DNS verified! Your portal is live at https://nocturne.ssuet.edu.pk"
  - Timeout: "DNS not yet propagated. You can continue setup and verify later."
- Option to skip and verify later (non-blocking)

### Step 7 — License Activation
Fields:
- **License Key** (text input, required)
  - Option A: Raw paste — user types or pastes the key directly
  - Option B: Upload license file — user uploads a `.aegis` license file (JSON, cryptographically signed by Nocturne Systems)
  - Toggle between both options
- **Validate License** button — calls Nocturne Systems license validation server
  - On success: show license details:
    - University name (must match Step 2 — if mismatch, reject with clear error)
    - Licensed modules (list with green checkmarks)
    - License expiry date
    - Max concurrent users
  - On failure: show exact rejection reason (invalid key / university mismatch / expired / revoked)
- Do NOT complete wizard until license is validated

### Completion Screen
- "Nocturne is ready."
- Summary card showing:
  - University name and logo
  - Portal URL
  - Licensed modules
  - License expiry
  - DB and Redis status (green)
- Single CTA: "Go to Admin Login"
- Redirect to `/admin/login`

---

## Technical Implementation Requirements

### Wizard State Management
- Wizard state persists across browser refresh (store in server-side session or temporary DB table `wizard_sessions`)
- If user closes browser mid-wizard and returns, resume from last completed step
- Wizard is inaccessible once `is_configured = true` flag is set in DB — redirect to login

### Configuration Storage
- All wizard outputs stored in a `system_config` table: `key | value | is_encrypted | created_at | updated_at`
- Sensitive values (connection strings, secrets) encrypted at rest using AES-256
- Encryption key injected via `NOCTURNE_ENCRYPTION_KEY` environment variable

### Security
- Wizard endpoint is completely unauthenticated (no login required — it runs before any user exists)
- After wizard completion, wizard route returns 404 permanently
- Rate limit wizard endpoints: max 10 requests per minute per IP

### Theming
- University primary/secondary colours from Step 2 applied immediately across the UI
- University logo replaces Nocturne logo in the top bar after Step 2 is saved
- All subsequent Nocturne screens use these colours and logo — fully white-labelled

### Error Handling
- Every step has a "Previous" button (except Step 1)
- Every connection test failure shows the exact technical error plus a plain-English explanation
- No step swallows exceptions silently

---

## Do NOT
- Hardcode any university name, URL, colour, or identifier anywhere in wizard code
- Store plaintext secrets in the database
- Allow the wizard to be accessed after setup is complete
- Skip connection tests — they are mandatory gates
- Assume any cloud provider — all storage/cache/DB choices are user-selected
