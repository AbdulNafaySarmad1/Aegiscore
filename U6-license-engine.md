# AegisCore — License Engine Prompt

## Role
You are a .NET 10 / C# 14 engineer building the license validation and enforcement engine for AegisCore. This is not optional — every running instance phones home to AegisCore Systems license server at 08:00 every morning. If the heartbeat fails, the system locks itself.

## Architecture

### License Server (AegisCore Systems side)
You maintain a separate service (can be same codebase, separate API endpoint, or separate service entirely) that:
- Issues cryptographically signed license keys
- Stores license state (active / revoked / expired)
- Accepts heartbeat pings from university instances
- Returns heartbeat response (valid / invalid / revoked / expired)

### University Instance (AegisCore side)
- Contains only a **license validator** — it does not generate keys
- Stores a copy of the license key
- Runs a daily heartbeat scheduler at 08:00 AM
- Locks system if heartbeat fails 2 days in a row
- Can be manually overridden by you (Abdul) via a bypass endpoint

---

## Task 1 — License Key Format

License key is a **JWT or cryptographically signed JSON blob** (your choice). Must include:

```json
{
  "university_id": "uuid",
  "university_name": "SSUET",
  "issued_date": "2024-01-01",
  "expiry_date": "2024-12-31",
  "max_concurrent_users": 1000,
  "licensed_modules": [
    "academic_calendar",
    "admissions",
    "examination",
    "hrcore",
    "finance",
    "obeframework",
    "student_portal",
    "faculty_portal"
  ],
  "issued_by": "apex-systems",
  "signature": "..." // HMAC-SHA256 or RSA signature
}
```

---

## Task 2 — License Storage & Validation

### Database Table
```sql
CREATE TABLE license_state (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    license_key TEXT NOT NULL,
    university_id UUID NOT NULL,
    university_name VARCHAR(200) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'active', -- active | revoked | expired | pending_activation
    license_json JSONB NOT NULL,
    activated_at TIMESTAMPTZ,
    last_heartbeat_at TIMESTAMPTZ,
    last_heartbeat_status VARCHAR(20), -- success | failed | timeout
    failed_heartbeat_count INT NOT NULL DEFAULT 0,
    locked_at TIMESTAMPTZ,
    lock_reason TEXT,
    override_by UUID, -- user who manually overrode a lock
    override_reason TEXT,
    override_until TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Service Interface
```csharp
public interface ILicenseService
{
    Task<LicenseValidationResult> ValidateAsync(string licenseKey, CancellationToken ct = default);
    Task ActivateAsync(string licenseKey, Guid universityId, CancellationToken ct = default);
    Task<HeartbeatResult> SendHeartbeatAsync(CancellationToken ct = default);
    Task<bool> IsModuleEnabledAsync(string moduleName, CancellationToken ct = default);
    Task<LicenseStatus> GetStatusAsync(CancellationToken ct = default);
    Task ManualOverrideAsync(Guid actorId, string reason, DateTimeOffset validUntil, CancellationToken ct = default);
}
```

### Validation Logic
```csharp
public class LicenseValidationResult
{
    public bool IsValid { get; set; }
    public string ValidationMessage { get; set; }
    public LicenseInfo License { get; set; }
    public LicenseValidationFailureReason? FailureReason { get; set; }
}

public enum LicenseValidationFailureReason
{
    InvalidSignature,
    Expired,
    Revoked,
    UniversityMismatch,
    CouldNotContactServer
}
```

---

## Task 3 — Daily Heartbeat Scheduler

### Hosted Service
```csharp
public class LicenseHeartbeatHostedService : BackgroundService
{
    private static TimeSpan HEARTBEAT_TIME = new TimeSpan(8, 0, 0); // 08:00 AM
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextHeartbeat = DateTime.Today.Add(HEARTBEAT_TIME);
            
            if (now > nextHeartbeat)
                nextHeartbeat = nextHeartbeat.AddDays(1);
            
            var delay = nextHeartbeat - now;
            await Task.Delay(delay, stoppingToken);
            
            await _licenseService.SendHeartbeatAsync(stoppingToken);
        }
    }
}
```

### Heartbeat Logic
1. At 08:00 AM exactly (within 1-minute window), make HTTPS POST to AegisCore Systems license server:
   ```
   POST /api/license/heartbeat
   {
     "university_id": "...",
     "university_name": "...",
     "current_user_count": X,
     "active_modules": ["..."],
     "timestamp": "ISO8601"
   }
   ```

2. License server responds with:
   ```json
   {
     "status": "valid" | "revoked" | "expired",
     "message": "...",
     "timestamp": "ISO8601"
   }
   ```

3. If response status is **valid**:
   - Update `license_state.last_heartbeat_at = NOW()`
   - Reset `failed_heartbeat_count = 0`
   - Log success

4. If response status is **revoked** or **expired**:
   - Update `license_state.status = revoked | expired`
   - Set `license_state.locked_at = NOW()`
   - Trigger **system lockout** (see Task 4)

5. If request **times out or fails** (no response, 5xx error):
   - Increment `failed_heartbeat_count++`
   - If `failed_heartbeat_count >= 2`: trigger **system lockout**
   - If `failed_heartbeat_count == 1`: log warning but allow system to continue
   - Log the failure with exact error reason

6. Heartbeat timeout: 10 seconds (configurable via policy config)
7. Retry: do NOT retry the same heartbeat request — wait until next day's 08:00 AM

---

## Task 4 — System Lockout

When the system is locked (`license_state.locked_at IS NOT NULL`):

- [ ] Every HTTP request to ANY endpoint (except `/api/license/override` and `/health`) returns **HTTP 403 Forbidden**
  ```json
  {
    "error": "System License Invalid",
    "reason": "revoked" | "expired" | "heartbeat_failed",
    "locked_at": "ISO8601",
    "contact": "Please contact support"
  }
  ```

- [ ] Frontend redirects to a lockout page showing:
  - License status
  - Reason for lockout
  - Last successful heartbeat date/time
  - "Contact Administrator" message with your email

- [ ] Background services (scheduled jobs, async tasks) are also blocked — check license status before executing

### Middleware
```csharp
app.UseMiddleware<LicenseLockoutMiddleware>();
```

---

## Task 5 — Manual Override (Abdul only)

Endpoint: `POST /api/admin/license/override` (requires SuperAdmin role)

Request:
```json
{
  "reason": "Temporary network outage at university",
  "valid_until": "2024-02-01T08:00:00Z"
}
```

Logic:
- Set `license_state.override_by = current_user_id`
- Set `license_state.override_reason = reason`
- Set `license_state.override_until = valid_until`
- Clear lockout: set `license_state.locked_at = NULL`
- Allow system to run normally until `override_until` expires
- At `override_until` expiry, if heartbeat still hasn't succeeded, re-lock automatically
- Log this action in audit log

---

## Task 6 — Module Gating

Every major feature guarded by module availability:

```csharp
[RequireModule("admissions")]
public async Task<IActionResult> CreateAdmissionAsync(...)
{
    // only if license includes "admissions" module
}
```

Implementation:
- `[RequireModule]` attribute checks `license_json.licensed_modules` array
- Returns **HTTP 403** with message: "This module is not included in your license. Contact AegisCore Systems to upgrade."
- Cache module check results in Redis (invalidate on license change)

---

## Task 7 — License Server Endpoint (AegisCore Systems)

You maintain a separate endpoint (or service) that responds to heartbeat requests:

```csharp
[HttpPost("/api/license/heartbeat")]
public async Task<IActionResult> ValidateHeartbeatAsync(HeartbeatRequest req)
{
    // Lookup license in AegisCore Systems database
    var license = await _db.Licenses.FirstOrDefaultAsync(
        l => l.UniversityId == req.UniversityId
    );
    
    if (license == null)
        return Ok(new { status = "invalid", message = "License not found" });
    
    if (license.ExpiryDate < DateTime.UtcNow)
        return Ok(new { status = "expired", message = "License expired" });
    
    if (license.IsRevoked)
        return Ok(new { status = "revoked", message = "License revoked" });
    
    if (req.CurrentUserCount > license.MaxConcurrentUsers)
        return Ok(new { status = "valid", message = "OK", warning = "User count exceeded" });
    
    return Ok(new { status = "valid", message = "OK" });
}
```

---

## Task 8 — Testing

- [ ] Unit test: valid license passes validation
- [ ] Unit test: expired license fails validation
- [ ] Unit test: revoked license fails validation
- [ ] Unit test: invalid signature fails validation
- [ ] Integration test: heartbeat succeeds and updates last_heartbeat_at
- [ ] Integration test: heartbeat timeout after 2 failures triggers lockout
- [ ] Integration test: all endpoints return 403 when locked
- [ ] Integration test: manual override clears lockout
- [ ] Load test: heartbeat at 08:00 AM under 100 concurrent instances (no race conditions)

---

## Do NOT
- Store plaintext license keys in code
- Bypass signature validation even for "testing"
- Allow heartbeat to be triggered manually by users (only scheduled, or SuperAdmin for manual override)
- Retry heartbeat immediately after failure — always wait until next day
- Log the full license key anywhere (log last 8 chars only)
