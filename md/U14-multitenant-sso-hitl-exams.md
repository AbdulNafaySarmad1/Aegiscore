# Nocturne — Multi-Tenant SSO, 2FA Enforcement & Agentic HITL Exam Pipeline (U14)

## Role
You are a security architect + AI engineer designing:
1. **Multi-Tenant SSO** — Microsoft Entra ID, Google Workspace, per-university branding
2. **Mandatory 2FA** — Generated 16-char password, forced 2FA on first login
3. **Agentic Human-in-the-Loop (HITL)** — AI agents grade papers, humans review edge cases
4. **Paper Digitization Pipeline** — Scan → OCR → AI → Human Review → Incinerate → Audit

---

## Part A — Multi-Tenant SSO Architecture

### The Problem: 500 Universities, 500 Different Auth Providers

```
University A uses: Microsoft Entra ID (Office 365)
University B uses: Google Workspace
University C uses: Okta
University D uses: Custom LDAP
University E uses: Nothing (Nocturne handles auth)

How do we support all without hardcoding?
```

### Solution: Multi-Tenant SSO Gateway

```
Nocturne.university.edu.pk/login
    ↓
    "Which university are you from?"
    ↓
User enters: University email (e.g., student@university.edu.pk)
    ↓
Nocturne looks up: What's the domain? university.edu.pk
    ↓
Query: SELECT sso_provider FROM universities WHERE domain = 'university.edu.pk'
    ↓
Result: "microsoft" (this university uses Microsoft Entra ID)
    ↓
Redirect to: Microsoft Entra ID login
    ↓
User authenticates with Microsoft (MFA already required by Microsoft)
    ↓
Microsoft redirects back: Here's the JWT token with claims:
    {
      "oid": "12345",
      "email": "student@university.edu.pk",
      "name": "Ahmed Khan",
      "university": "SSUET"
    }
    ↓
Nocturne validates: Yes, this Microsoft ID is legitimate
    ↓
Nocturne creates session: First-time login?
    ├─ YES: Force 2FA setup + password change
    └─ NO: Redirect to dashboard
```

### Database Schema

```sql
CREATE TABLE universities (
    id UUID PRIMARY KEY,
    code VARCHAR(20) NOT NULL UNIQUE, -- "SSUET", "LUMS", "FAST"
    name VARCHAR(200) NOT NULL,
    domain VARCHAR(100) NOT NULL UNIQUE, -- university.edu.pk
    logo_url TEXT, -- For login page branding
    
    -- SSO Configuration
    sso_provider ENUM('microsoft', 'google', 'okta', 'ldap', 'nocturne'),
    sso_tenant_id TEXT, -- For Microsoft Entra ID
    sso_client_id TEXT,
    sso_client_secret TEXT ENCRYPTED, -- AES-256
    
    -- Mandatory 2FA
    require_2fa BOOLEAN DEFAULT true,
    require_password_change_on_first_login BOOLEAN DEFAULT true,
    
    -- Branding
    primary_color VARCHAR(7), -- #FF6B6B
    secondary_color VARCHAR(7),
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE users (
    id UUID PRIMARY KEY,
    university_id UUID NOT NULL REFERENCES universities(id),
    
    -- Basic info
    email VARCHAR(255) NOT NULL,
    full_name VARCHAR(200),
    role ENUM('student', 'faculty', 'hod', 'vc', 'admin'),
    
    -- SSO Identity
    sso_provider VARCHAR(50), -- "microsoft", "google", etc.
    sso_id VARCHAR(500), -- External ID from SSO provider
    sso_email VARCHAR(255), -- May differ from Nocturne email
    
    -- Authentication
    password_hash VARCHAR(255), -- Bcrypt (for Nocturne auth only)
    password_changed_at TIMESTAMPTZ,
    password_expires_at TIMESTAMPTZ,
    temporary_password BOOLEAN DEFAULT false, -- First login?
    
    -- 2FA
    two_fa_enabled BOOLEAN DEFAULT false,
    two_fa_method ENUM('authenticator', 'email', 'sms'),
    two_fa_secret VARCHAR(255) ENCRYPTED, -- For TOTP
    two_fa_verified_at TIMESTAMPTZ,
    two_fa_backup_codes TEXT[] ENCRYPTED, -- For recovery
    
    -- Status
    is_active BOOLEAN DEFAULT true,
    first_login_at TIMESTAMPTZ,
    last_login_at TIMESTAMPTZ,
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    UNIQUE(university_id, email),
    UNIQUE(university_id, sso_provider, sso_id)
);

CREATE TABLE sso_sessions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    
    -- Session info
    access_token TEXT ENCRYPTED,
    refresh_token TEXT ENCRYPTED,
    token_expires_at TIMESTAMPTZ,
    
    -- Device info
    device_id VARCHAR(255),
    device_name VARCHAR(200),
    ip_address INET,
    user_agent TEXT,
    
    -- Status
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ
);
```

### Multi-Tenant SSO Flow (C#/.NET)

```csharp
[ApiController]
[Route("api/auth")]
public class MultiTenantAuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfigurationManager _config;
    private readonly IMicrosoftGraphClient _msGraph;
    private readonly IGoogleAuthClient _googleAuth;
    private readonly ILogger<MultiTenantAuthController> _logger;
    
    // Step 1: User enters email → Determine SSO provider
    [HttpPost("detect-provider")]
    public async Task<IActionResult> DetectSSoProvider(
        [FromBody] EmailDetectionRequest request,
        CancellationToken ct = default)
    {
        // Extract domain from email
        var emailParts = request.Email.Split('@');
        if (emailParts.Length != 2)
            return BadRequest("Invalid email format");
        
        var domain = emailParts[1];
        
        // Look up university by domain
        var university = await _db.Universities
            .FirstOrDefaultAsync(u => u.Domain == domain, ct);
        
        if (university == null)
            return NotFound(new { error = "University not found" });
        
        // Return SSO provider + branding
        return Ok(new
        {
            universityId = university.Id,
            universityName = university.Name,
            ssoProvider = university.SsoProvider.ToString(),
            logoUrl = university.LogoUrl,
            primaryColor = university.PrimaryColor,
            secondaryColor = university.SecondaryColor,
            redirectUrl = GetSsoRedirectUrl(university)
        });
    }
    
    // Step 2: Redirect to SSO provider (Microsoft, Google, etc.)
    private string GetSsoRedirectUrl(University university)
    {
        return university.SsoProvider switch
        {
            SsoProvider.Microsoft => BuildMicrosoftAuthUrl(university),
            SsoProvider.Google => BuildGoogleAuthUrl(university),
            SsoProvider.Okta => BuildOktaAuthUrl(university),
            _ => throw new InvalidOperationException()
        };
    }
    
    // Step 3: SSO provider redirects back with token
    [HttpPost("sso-callback")]
    public async Task<IActionResult> SsoCallback(
        [FromBody] SsoCallbackRequest request,
        CancellationToken ct = default)
    {
        // Validate SSO token signature
        var claims = ValidateSsoToken(request.Token, request.University);
        
        var email = claims["email"].ToString();
        var ssoId = claims["oid"].ToString(); // or "sub" for Google
        var ssoProvider = request.SsoProvider;
        
        // Find or create user
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UniversityId == request.UniversityId
                && u.SsoProvider == ssoProvider.ToString()
                && u.SsoId == ssoId, ct);
        
        if (user == null)
        {
            // First time login: create user with temporary password
            user = new User
            {
                Id = Guid.NewGuid(),
                UniversityId = request.UniversityId,
                Email = email,
                FullName = claims["name"].ToString(),
                SsoProvider = ssoProvider.ToString(),
                SsoId = ssoId,
                SsoEmail = email,
                Role = DetermineRole(email, request.UniversityId), // student, faculty, etc.
                TemporaryPassword = true, // Force password change + 2FA
                FirstLoginAt = DateTime.UtcNow
            };
            
            // Generate 16-char complex password
            user.PasswordHash = GenerateTemporaryPassword();
            
            _db.Users.Add(user);
        }
        
        // Create session
        var session = new SsoSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AccessToken = request.Token,
            RefreshToken = request.RefreshToken,
            TokenExpiresAt = DateTime.UtcNow.AddHours(1),
            DeviceId = request.DeviceId,
            DeviceName = request.DeviceName,
            IpAddress = HttpContext.Connection.RemoteIpAddress,
            UserAgent = Request.Headers["User-Agent"],
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        
        _db.SsoSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        
        // Check: Is this first login?
        if (user.TemporaryPassword)
        {
            // Force 2FA setup + password change
            return Ok(new
            {
                status = "first_login",
                sessionId = session.Id,
                redirect = "/auth/setup-2fa" // Redirect to 2FA setup page
            });
        }
        
        // Check: Is 2FA enabled?
        if (user.TwoFaEnabled)
        {
            // Request 2FA verification
            return Ok(new
            {
                status = "2fa_required",
                sessionId = session.Id,
                redirect = "/auth/verify-2fa"
            });
        }
        
        // All good: return session token
        return Ok(new
        {
            status = "authenticated",
            sessionId = session.Id,
            redirect = "/"
        });
    }
    
    // Step 4: On first login, force 2FA setup
    [HttpPost("setup-2fa")]
    [Authorize]
    public async Task<IActionResult> Setup2Fa(
        [FromBody] Setup2FaRequest request,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync(userId, ct);
        
        if (!user.TemporaryPassword)
            return BadRequest("2FA setup already completed");
        
        // Generate 2FA secret (TOTP authenticator app)
        var secret = GenerateTotpSecret();
        var qrCode = GenerateQrCode(secret, user.Email);
        
        return Ok(new
        {
            secret = secret, // User scans this into Google Authenticator
            qrCode = qrCode, // Display as QR code
            backupCodes = GenerateBackupCodes(10), // Recovery codes
            message = "Scan QR code into Google Authenticator or Microsoft Authenticator"
        });
    }
    
    // Step 5: Verify 2FA setup + change password
    [HttpPost("complete-2fa-setup")]
    [Authorize]
    public async Task<IActionResult> Complete2FaSetup(
        [FromBody] Complete2FaSetupRequest request,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync(userId, ct);
        
        // Verify TOTP code
        if (!VerifyTotpCode(request.TotpCode, request.Secret))
            return BadRequest("Invalid TOTP code");
        
        // Verify password change
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("New password required");
        
        if (!IsPasswordComplex(request.NewPassword))
            return BadRequest("Password must be 12+ chars, uppercase, lowercase, numbers, special");
        
        // Update user
        user.TwoFaEnabled = true;
        user.TwoFaMethod = "authenticator";
        user.TwoFaSecret = Encrypt(request.Secret);
        user.TwoFaBackupCodes = request.BackupCodes.Select(Encrypt).ToArray();
        user.TwoFaVerifiedAt = DateTime.UtcNow;
        
        user.PasswordHash = HashPassword(request.NewPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.TemporaryPassword = false; // No longer temporary
        
        await _db.SaveChangesAsync(ct);
        
        return Ok(new { message = "2FA setup complete" });
    }
    
    // Step 6: On every login, verify 2FA
    [HttpPost("verify-2fa")]
    public async Task<IActionResult> Verify2Fa(
        [FromBody] Verify2FaRequest request,
        CancellationToken ct = default)
    {
        var session = await _db.SsoSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);
        
        if (session == null)
            return Unauthorized();
        
        var user = session.User;
        
        // Verify TOTP code
        var secret = Decrypt(user.TwoFaSecret);
        bool isValid = VerifyTotpCode(request.TotpCode, secret);
        
        // Fallback: check backup codes
        if (!isValid && !string.IsNullOrEmpty(request.BackupCode))
        {
            isValid = VerifyBackupCode(request.BackupCode, user.TwoFaBackupCodes);
        }
        
        if (!isValid)
            return Unauthorized("Invalid 2FA code");
        
        // Update session: 2FA verified
        session.IsActive = true;
        await _db.SaveChangesAsync(ct);
        
        return Ok(new
        {
            message = "2FA verified",
            sessionId = session.Id,
            redirect = "/"
        });
    }
    
    // Helper: Determine role from email
    private string DetermineRole(string email, Guid universityId)
    {
        // Logic: if email domain is faculty@university.edu.pk → faculty
        // if email is in student list → student
        // etc.
        
        var localPart = email.Split('@')[0];
        
        if (localPart.Contains("faculty") || localPart.Contains("prof"))
            return "faculty";
        if (localPart.Contains("hod"))
            return "hod";
        if (localPart.Contains("admin"))
            return "admin";
        
        return "student"; // Default
    }
    
    // Helper: Generate 16-char complex password
    private string GenerateTemporaryPassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*-=+";
        
        var password = new List<char>();
        var random = new Random();
        
        // Ensure complexity: 1 upper, 1 lower, 1 digit, 1 special, rest random
        password.Add(uppercase[random.Next(uppercase.Length)]);
        password.Add(lowercase[random.Next(lowercase.Length)]);
        password.Add(digits[random.Next(digits.Length)]);
        password.Add(special[random.Next(special.Length)]);
        
        // Fill remaining 12 chars randomly
        var allChars = uppercase + lowercase + digits + special;
        for (int i = 0; i < 12; i++)
        {
            password.Add(allChars[random.Next(allChars.Length)]);
        }
        
        // Shuffle
        return new string(password.OrderBy(_ => random.Next()).ToArray());
    }
}
```

### University-Specific Login Page

```html
<!-- login.html (Dynamic branding) -->
<!DOCTYPE html>
<html>
<head>
    <title>Nocturne Login</title>
    <style>
        :root {
            --primary-color: var(--university-primary);
            --secondary-color: var(--university-secondary);
        }
        
        .login-container {
            background: linear-gradient(135deg, var(--primary-color), var(--secondary-color));
            display: flex;
            align-items: center;
            justify-content: center;
            height: 100vh;
        }
        
        .university-logo {
            max-width: 200px;
            margin-bottom: 30px;
        }
        
        .university-name {
            font-size: 24px;
            color: var(--primary-color);
            font-weight: bold;
        }
    </style>
</head>
<body>
    <div class="login-container">
        <div class="login-card">
            <img id="universityLogo" class="university-logo" alt="University Logo">
            <h1 id="universityName" class="university-name"></h1>
            
            <form id="loginForm">
                <input type="email" id="email" placeholder="Enter your university email" required>
                <button type="button" onclick="detectProvider()">Next</button>
            </form>
        </div>
    </div>
    
    <script>
        async function detectProvider() {
            const email = document.getElementById('email').value;
            
            const response = await fetch('/api/auth/detect-provider', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email })
            });
            
            const data = await response.json();
            
            // Update page with university branding
            document.getElementById('universityLogo').src = data.logoUrl;
            document.getElementById('universityName').textContent = data.universityName;
            
            // Apply colors
            document.documentElement.style.setProperty('--university-primary', data.primaryColor);
            document.documentElement.style.setProperty('--university-secondary', data.secondaryColor);
            
            // Redirect to SSO provider
            window.location.href = data.redirectUrl;
        }
    </script>
</body>
</html>
```

---

## Part B — Mandatory 2FA Enforcement

### Flow Diagram

```
User logs in via SSO
    ↓
    First time?
    ├─ YES: Redirect to /auth/setup-2fa
    │   ├─ Generate TOTP secret
    │   ├─ Display QR code
    │   ├─ User scans into Google Authenticator
    │   ├─ User enters 6-digit code
    │   ├─ User creates new password (must be complex)
    │   └─ 2FA enabled ✅
    │
    └─ NO: Continue to next check
       ↓
       2FA enabled?
       ├─ YES: Request 2FA verification
       │   ├─ User enters 6-digit code from authenticator
       │   ├─ OR enters backup code (if lost phone)
       │   └─ Login successful ✅
       │
       └─ NO: FORCE 2FA NOW
           └─ Redirect to /auth/setup-2fa
```

### 2FA Verification Middleware (per request)

```csharp
public class TwoFaVerificationMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        var userId = context.User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            await _next(context);
            return;
        }
        
        var user = await db.Users.FindAsync(Guid.Parse(userId));
        
        // Skip 2FA check for certain paths
        var skipPaths = new[] { "/api/auth/", "/health", "/metrics" };
        if (skipPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
        {
            await _next(context);
            return;
        }
        
        // Check: Is 2FA verified in current session?
        var session = await db.SsoSessions
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
        
        if (!user.TwoFaEnabled)
        {
            // Force 2FA setup
            context.Response.Redirect("/auth/setup-2fa");
            return;
        }
        
        // Check: Has 2FA been verified in this session?
        var twoFaVerified = context.User.FindFirst("2fa_verified")?.Value == "true";
        
        if (!twoFaVerified)
        {
            context.Response.Redirect("/auth/verify-2fa");
            return;
        }
        
        await _next(context);
    }
}
```

---

## Part C — Agentic HITL Exam Paper Pipeline

### Complete Paper Digitization Flow

```
PHYSICAL WORLD
├─ Stack of exam papers (500 papers for CS101 midterm)
│
├─ Step 1: Remove Fasteners
│  ├─ Remove staples
│  ├─ Remove paperclips
│  └─ Remove sticky notes
│
├─ Step 2: Batch & Track
│  ├─ Group 50 papers into Bin #1
│  ├─ Assign batch ID: BATCH-2024-CS101-001
│  └─ Print tracking sheet:
│     ├─ Batch ID
│     ├─ Course: CS101
│     ├─ Exam: Midterm 1
│     ├─ Expected page count: 4 pages × 50 papers = 200 pages
│     └─ Operator: Ahmed Khan
│
├─ Step 3: Embed Identifiers
│  ├─ Print unique QR code on front page of each paper
│  │  ├─ QR code = { studentId, paperId, sequence }
│  │  └─ Example: QR(ABC123-P001-1)
│  └─ Geometrical patch codes (for scanner recognition)
│
├─ Step 4: High-Speed Scanning
│  ├─ Load all 200 pages into ADF scanner
│  ├─ Settings:
│  │  ├─ Duplex (both sides)
│  │  ├─ 300 DPI
│  │  ├─ Color
│  │  ├─ Ultrasonic double-feed detection (ENABLED)
│  │  └─ Auto-rotate
│  │
│  ├─ Scanner processes all 200 pages (~5 min)
│  │  ├─ Detects any jams (stops, alerts operator)
│  │  ├─ Stamps audit trail on back of each page
│  │  │  └─ Ink mark: [BATCH-2024-CS101-001] [Page 45/200] [2024-01-15 10:32]
│  │  └─ Outputs PDF stream
│  │
│  └─ Output: raw-scan-batch-001.pdf (200 pages)
│
├─ Step 5: Auto-Split by QR Code
│  ├─ Computer vision script reads QR codes
│  │  ├─ Detects: Paper 1 (ABC123) Pages 1-4
│  │  ├─ Detects: Paper 2 (XYZ789) Pages 5-8
│  │  └─ Detects: Paper 3 (DEF456) Pages 9-12
│  │
│  └─ Auto-split PDF:
│     ├─ ABC123-CS101-Midterm1.pdf (4 pages)
│     ├─ XYZ789-CS101-Midterm1.pdf (4 pages)
│     └─ DEF456-CS101-Midterm1.pdf (4 pages)
│
├─ Step 6: Page Count Reconciliation
│  ├─ Compare:
│  │  ├─ Expected: 4 pages × 50 students = 200 pages
│  │  ├─ Scanned: 200 pages ✅ MATCH
│  │  └─ If mismatch: Alert operator, re-scan
│  │
│  └─ Generate report: page-count-reconciliation.csv
│     ├─ StudentID | Expected Pages | Actual Pages | Status
│     ├─ ABC123 | 4 | 4 | ✅
│     ├─ XYZ789 | 4 | 4 | ✅
│     └─ ...
│
├─ Step 7: Blur & Blank Check (Computer Vision)
│  ├─ For each page:
│  │  ├─ Check: Is image readable? (sharpness > 0.7)
│  │  ├─ Check: Is page blank? (blank_percentage < 5%)
│  │  └─ Check: Contains text? (confidence > 0.9)
│  │
│  └─ Flag issues:
│     ├─ ABC123 Page 2: BLURRY (sharpness: 0.45) → Needs human review
│     ├─ DEF456 Page 3: BLANK (blank_percentage: 95%) → Needs human review
│     └─ ...
│
├─ Step 8: OCR Extraction (Azure Computer Vision)
│  ├─ For each page:
│  │  ├─ Extract text via OCR
│  │  ├─ Measure confidence: 0.0-1.0
│  │  └─ Flag low-confidence sections
│  │
│  └─ Example output:
│     {
│       "studentId": "ABC123",
│       "pageNumber": 1,
│       "rawText": "Question 1: Define operating system...",
│       "confidence": 0.92,
│       "unconfidentSections": [
│         { "text": "???", "confidence": 0.45 }
│       ]
│     }
│
├─ Step 9: Issue Destruction Manifest
│  ├─ Once all quality checks pass:
│  │  ├─ Generate manifest: destruction-manifest-batch-001.pdf
│  │  │  ├─ Batch ID
│  │  │  ├─ Item count: 50 papers
│  │  │  ├─ Total pages: 200
│  │  │  ├─ OCR confidence: Avg 0.91
│  │  │  ├─ Issues flagged: 3 (manual review required)
│  │  │  ├─ Digital archive: ✅ Verified
│  │  │  ├─ Hash (SHA-256 of all PDFs): abc123def456...
│  │  │  ├─ Timestamp: 2024-01-15 10:45:00 UTC
│  │  │  └─ Authorized by: System (auto)
│  │  │
│  │  └─ Print manifest (2 copies)
│  │     ├─ Copy 1: Tape to incineration container
│  │     └─ Copy 2: File in records
│  │
│  └─ Database entry:
│     INSERT destruction_log VALUES (
│       batch_id: 'BATCH-2024-CS101-001',
│       item_count: 50,
│       status: 'ready_for_destruction',
│       manifest_hash: 'abc123def456...',
│       created_at: NOW()
│     )
│
└─ Step 10: Incinerate Physical Papers
   ├─ Operator picks up incineration container
   ├─ Scans manifest QR code
   ├─ System records: "Destruction initiated"
   ├─ Operator feeds papers into industrial shredder/incinerator
   ├─ Shredder completes (5-10 min)
   ├─ Operator scans manifest again: "Destruction complete"
   │
   └─ System records:
      ├─ destroyed_at: NOW()
      ├─ operator_id: 'OP-Ahmed-Khan'
      ├─ destruction_machine: 'Shredder-A1'
      ├─ log_entry: "50 papers, 200 pages incinerated"
      └─ STATUS: ✅ ARCHIVED (digital) + ✅ DESTROYED (physical)


DIGITAL WORLD (Parallel)
├─ Step 11: Agentic Grading (AI Agents)
│  ├─ For each student paper:
│  │  ├─ Route to specialized AI agents:
│  │  │  ├─ Agent 1: Grade Problem 1 (20 points)
│  │  │  ├─ Agent 2: Grade Problem 2 (30 points)
│  │  │  ├─ Agent 3: Grade Problem 3 (20 points)
│  │  │  ├─ Agent 4: Grade Problem 4 (30 points)
│  │  │  └─ Supervisor Agent: Aggregate scores, return final grade
│  │  │
│  │  ├─ Each agent:
│  │  │  ├─ Reads OCR text + image
│  │  │  ├─ Evaluates against rubric
│  │  │  ├─ Assigns score (0-20, 0-30, etc.)
│  │  │  ├─ Records confidence (0.0-1.0)
│  │  │  └─ Outputs: { score: 18, confidence: 0.87, reasoning: "..." }
│  │  │
│  │  └─ Supervisor aggregates:
│  │     ├─ Total score: 18 + 28 + 17 + 27 = 90/100
│  │     ├─ Overall confidence: (0.87 + 0.91 + 0.79 + 0.88) / 4 = 0.86
│  │     └─ Outputs: { final_score: 90, confidence: 0.86 }
│  │
│  └─ Grade database entry:
│     INSERT exam_grades VALUES (
│       student_id: 'ABC123',
│       course_id: 'CS101',
│       exam_id: 'Midterm1',
│       ai_score: 90,
│       ai_confidence: 0.86,
│       status: 'pending_human_review'
│     )
│
└─ Step 12: Trigger Human Review (Agentic HITL)
   ├─ Rules: Route to human if:
   │  ├─ OCR confidence < 0.8 (text unclear)
   │  ├─ AI confidence < 0.75 (AI unsure)
   │  ├─ AI score is edge case (95-100, 0-5)
   │  ├─ AI score difference > 10 points between agents
   │  └─ Manual flag (faculty override request)
   │
   └─ Human Review Dashboard:
      ├─ Query: SELECT * FROM exam_grades WHERE status = 'pending_human_review'
      ├─ Display to faculty:
      │  ├─ Student name: Ahmed Khan
      │  ├─ Original paper (PDF): [Link]
      │  ├─ OCR text: [Show extracted text]
      │  ├─ AI score: 90
      │  ├─ AI confidence: 0.86
      │  ├─ AI reasoning: "Problem 1: Correct algorithm, minor syntax error (-2 points)"
      │  ├─ Why flagged: "OCR confidence 0.75 on page 2"
      │  │
      │  └─ Human action:
      │     ├─ [✅ APPROVE] AI score is correct → final_score = 90
      │     ├─ [📝 ADJUST] Change to 92 → AI was slightly harsh
      │     ├─ [❌ REJECT] AI completely wrong, grade as 75
      │     └─ [❓ UNCLEAR] Flag for department head review
      │
      └─ Update database:
         UPDATE exam_grades SET
         final_score = 92,
         human_review_score = 92,
         human_reviewer_id = 'PROF-Jane-Doe',
         status = 'graded'
         WHERE student_id = 'ABC123'
```

### Database Schema

```sql
CREATE TABLE exam_scans (
    id UUID PRIMARY KEY,
    university_id UUID NOT NULL REFERENCES universities(id),
    course_id UUID NOT NULL REFERENCES courses(id),
    exam_id UUID NOT NULL REFERENCES exams(id),
    
    -- Batch info
    batch_id VARCHAR(100) NOT NULL UNIQUE, -- BATCH-2024-CS101-001
    batch_sequence INT, -- Batch #1, #2, etc.
    
    -- Physical tracking
    operator_id UUID REFERENCES users(id),
    scan_started_at TIMESTAMPTZ,
    scan_completed_at TIMESTAMPTZ,
    
    -- Scan details
    total_pages INT,
    expected_pages INT,
    pages_match BOOLEAN,
    
    -- Quality metrics
    avg_blur_score DECIMAL(3,2),
    avg_blank_percentage DECIMAL(3,2),
    avg_ocr_confidence DECIMAL(3,2),
    
    -- Files
    raw_pdf_url TEXT, -- raw-scan-batch-001.pdf
    manifest_hash VARCHAR(255), -- SHA-256
    manifest_generated_at TIMESTAMPTZ,
    
    -- Destruction tracking
    destruction_status ENUM('pending', 'manifest_issued', 'destroyed'),
    destroyed_at TIMESTAMPTZ,
    destruction_log TEXT,
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE exam_papers (
    id UUID PRIMARY KEY,
    exam_scan_id UUID NOT NULL REFERENCES exam_scans(id),
    student_id UUID NOT NULL REFERENCES students(id),
    
    -- Paper details
    paper_sequence INT, -- Paper 1, 2, 3, ...
    page_count INT, -- 4 pages per paper
    page_range VARCHAR(20), -- "1-4" in scan
    
    -- Digital file
    pdf_url TEXT, -- ABC123-CS101-Midterm1.pdf
    pdf_hash VARCHAR(255), -- SHA-256 of PDF
    
    -- QR code data
    qr_code_data TEXT, -- { studentId, paperId, sequence }
    qr_detected BOOLEAN,
    
    -- Archive status
    archived_at TIMESTAMPTZ,
    archive_location TEXT, -- S3 bucket path
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE exam_grades (
    id UUID PRIMARY KEY,
    exam_paper_id UUID NOT NULL REFERENCES exam_papers(id),
    student_id UUID NOT NULL REFERENCES students(id),
    exam_id UUID NOT NULL REFERENCES exams(id),
    
    -- AI Grading
    ai_score DECIMAL(5,2),
    ai_confidence DECIMAL(3,2),
    ai_reasoning TEXT, -- JSON of agent outputs
    
    -- Quality flags
    ocr_confidence DECIMAL(3,2),
    blur_detected BOOLEAN,
    blank_detected BOOLEAN,
    flagged_for_review BOOLEAN,
    flag_reason TEXT, -- "OCR confidence < 0.8", etc.
    
    -- Human Review
    human_reviewed BOOLEAN DEFAULT false,
    human_reviewer_id UUID REFERENCES users(id),
    human_review_score DECIMAL(5,2),
    human_review_notes TEXT,
    human_reviewed_at TIMESTAMPTZ,
    
    -- Final grade
    final_score DECIMAL(5,2),
    grading_status ENUM('ai_graded', 'pending_human_review', 'human_graded'),
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE destruction_log (
    id UUID PRIMARY KEY,
    batch_id VARCHAR(100) NOT NULL UNIQUE REFERENCES exam_scans(batch_id),
    
    -- Manifest
    manifest_hash VARCHAR(255),
    manifest_issued_at TIMESTAMPTZ,
    
    -- Destruction
    destruction_operator_id UUID REFERENCES users(id),
    destruction_machine VARCHAR(100), -- "Shredder-A1"
    destruction_started_at TIMESTAMPTZ,
    destruction_completed_at TIMESTAMPTZ,
    
    -- Audit
    item_count INT,
    page_count INT,
    digital_archive_hash VARCHAR(255), -- SHA-256 of all student PDFs
    
    -- Status
    status ENUM('manifest_issued', 'destruction_in_progress', 'destroyed'),
    notes TEXT,
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Agentic Grading Implementation

```csharp
[ApiController]
[Route("api/grading")]
public class AgenticGradingController : ControllerBase
{
    private readonly IExamPaperRepository _papers;
    private readonly IGradingAgentOrchestrator _agentOrchestrator;
    private readonly IHumanReviewQueue _reviewQueue;
    
    [HttpPost("grade-batch/{examScanId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GradeBatch(
        Guid examScanId,
        CancellationToken ct = default)
    {
        var papers = await _papers.GetPapersByExamScanAsync(examScanId, ct);
        
        var results = new List<GradingResult>();
        
        foreach (var paper in papers)
        {
            // Step 1: Extract OCR text + metrics
            var ocrData = await ExtractOcrAsync(paper.PdfUrl, ct);
            
            // Step 2: Route to agentic grading
            var gradingResult = await _agentOrchestrator.GradeAsync(
                studentId: paper.StudentId,
                ocrText: ocrData.Text,
                rubric: paper.Exam.GradingRubric,
                ct: ct
            );
            
            // Step 3: Evaluate confidence
            var shouldFlagForReview = ShouldFlagForReview(
                ocrConfidence: ocrData.Confidence,
                aiConfidence: gradingResult.Confidence,
                aiScore: gradingResult.Score,
                rubric: paper.Exam.GradingRubric
            );
            
            // Step 4: Store result
            var grade = new ExamGrade
            {
                ExamPaperId = paper.Id,
                StudentId = paper.StudentId,
                ExamId = paper.ExamId,
                
                AiScore = gradingResult.Score,
                AiConfidence = gradingResult.Confidence,
                AiReasoning = JsonSerializer.Serialize(gradingResult.AgentOutputs),
                
                OcrConfidence = ocrData.Confidence,
                BlurDetected = ocrData.IsBlurry,
                BlankDetected = ocrData.IsBlank,
                
                FlaggedForReview = shouldFlagForReview,
                FlagReason = shouldFlagForReview ? GenerateFlagReason(ocrData, gradingResult) : null,
                
                GradingStatus = shouldFlagForReview ? "pending_human_review" : "ai_graded",
                FinalScore = shouldFlagForReview ? null : gradingResult.Score
            };
            
            await _papers.SaveGradeAsync(grade, ct);
            
            // Step 5: Queue for human review if needed
            if (shouldFlagForReview)
            {
                await _reviewQueue.EnqueueAsync(new HumanReviewItem
                {
                    ExamGradeId = grade.Id,
                    StudentId = paper.StudentId,
                    AiScore = gradingResult.Score,
                    AiConfidence = gradingResult.Confidence,
                    FlagReason = grade.FlagReason,
                    PaperUrl = paper.PdfUrl
                }, ct);
            }
            
            results.Add(new GradingResult
            {
                StudentId = paper.StudentId,
                Score = gradingResult.Score,
                Confidence = gradingResult.Confidence,
                FlaggedForReview = shouldFlagForReview
            });
        }
        
        return Ok(new
        {
            batchId = examScanId,
            totalPapers = papers.Count,
            graded = results.Count(r => !r.FlaggedForReview),
            flaggedForReview = results.Count(r => r.FlaggedForReview),
            results
        });
    }
    
    private bool ShouldFlagForReview(
        decimal ocrConfidence,
        decimal aiConfidence,
        decimal aiScore,
        GradingRubric rubric)
    {
        // Flag if:
        // - OCR is unclear
        if (ocrConfidence < 0.8m) return true;
        
        // - AI is unsure
        if (aiConfidence < 0.75m) return true;
        
        // - Score is edge case
        if (aiScore >= 95 || aiScore <= 5) return true;
        
        // - Score is close to pass/fail boundary
        var passingScore = rubric.PassingScore;
        if (Math.Abs(aiScore - passingScore) < 5) return true;
        
        return false;
    }
    
    [HttpGet("human-review-queue")]
    [Authorize(Roles = "Faculty, HOD")]
    public async Task<IActionResult> GetHumanReviewQueue(CancellationToken ct = default)
    {
        var items = await _reviewQueue.GetPendingAsync(
            departmentId: User.GetDepartmentId(),
            ct: ct
        );
        
        return Ok(items.Select(i => new
        {
            i.ExamGradeId,
            i.StudentId,
            i.StudentName,
            i.AiScore,
            i.AiConfidence,
            i.FlagReason,
            i.PaperUrl,
            i.CreatedAt
        }));
    }
    
    [HttpPost("human-review/{examGradeId}")]
    [Authorize(Roles = "Faculty, HOD")]
    public async Task<IActionResult> SubmitHumanReview(
        Guid examGradeId,
        [FromBody] HumanReviewRequest request,
        CancellationToken ct = default)
    {
        var grade = await _papers.GetGradeAsync(examGradeId, ct);
        
        grade.HumanReviewScore = request.Score;
        grade.HumanReviewNotes = request.Notes;
        grade.HumanReviewerId = User.GetUserId();
        grade.HumanReviewedAt = DateTime.UtcNow;
        grade.FinalScore = request.Score;
        grade.GradingStatus = "human_graded";
        
        await _papers.SaveGradeAsync(grade, ct);
        
        // Remove from review queue
        await _reviewQueue.RemoveAsync(examGradeId, ct);
        
        return Ok(new { message = "Review submitted" });
    }
}

// Agentic Orchestrator
public class GradingAgentOrchestrator
{
    private readonly IClaudeClient _claude;
    private readonly IGradingRubric _rubric;
    
    public async Task<GradingOutput> GradeAsync(
        Guid studentId,
        string ocrText,
        GradingRubric rubric,
        CancellationToken ct)
    {
        // Create specialized agents for each problem
        var agents = new List<GradingAgent>();
        
        foreach (var problem in rubric.Problems)
        {
            agents.Add(new GradingAgent
            {
                ProblemId = problem.Id,
                MaxScore = problem.MaxScore,
                Rubric = problem.Rubric
            });
        }
        
        // Grade each problem in parallel
        var agentTasks = agents.Select(async agent =>
        {
            var prompt = $@"
You are a strict exam grader. Grade this student's answer against the rubric.

OCR Text:
{ocrText}

Problem {agent.ProblemId}: {agent.Rubric}

Return ONLY JSON:
{{
  ""score"": <0-{agent.MaxScore}>,
  ""confidence"": <0.0-1.0>,
  ""reasoning"": ""why this score""
}}
";
            
            var response = await _claude.CompleteAsync(prompt, ct);
            return JsonSerializer.Deserialize<AgentGradingOutput>(response);
        });
        
        var results = await Task.WhenAll(agentTasks);
        
        // Supervisor agent aggregates
        var supervisorPrompt = $@"
You are a supervisor grading agent. Aggregate these individual grades.

Grades:
{JsonSerializer.Serialize(results)}

Max total: {rubric.Problems.Sum(p => p.MaxScore)}

Return JSON:
{{
  ""finalScore"": <0-{rubric.Problems.Sum(p => p.MaxScore)}>,
  ""confidence"": <0.0-1.0>,
  ""summary"": ""overall assessment""
}}
";
        
        var supervisorResponse = await _claude.CompleteAsync(supervisorPrompt, ct);
        var finalOutput = JsonSerializer.Deserialize<GradingOutput>(supervisorResponse);
        
        finalOutput.AgentOutputs = results; // Store individual agent outputs
        
        return finalOutput;
    }
}
```

### Human Review Dashboard (React)

```jsx
export function HumanReviewDashboard() {
    const [queue, setQueue] = useState([]);
    const [selectedItem, setSelectedItem] = useState(null);
    const [reviewScore, setReviewScore] = useState(null);
    const [reviewNotes, setReviewNotes] = useState("");
    
    useEffect(() => {
        async function fetchQueue() {
            const response = await fetch('/api/grading/human-review-queue');
            const data = await response.json();
            setQueue(data);
        }
        fetchQueue();
    }, []);
    
    const handleSubmitReview = async () => {
        const response = await fetch(
            `/api/grading/human-review/${selectedItem.examGradeId}`,
            {
                method: 'POST',
                body: JSON.stringify({
                    score: reviewScore,
                    notes: reviewNotes
                })
            }
        );
        
        if (response.ok) {
            // Remove from queue
            setQueue(queue.filter(i => i.examGradeId !== selectedItem.examGradeId));
            setSelectedItem(null);
        }
    };
    
    return (
        <div className="human-review-dashboard">
            <h1>Exam Review Queue ({queue.length})</h1>
            
            <div className="queue-list">
                {queue.map(item => (
                    <div
                        key={item.examGradeId}
                        className={`queue-item ${selectedItem?.examGradeId === item.examGradeId ? 'selected' : ''}`}
                        onClick={() => setSelectedItem(item)}
                    >
                        <div>
                            <strong>{item.studentName}</strong>
                            <p>AI Score: {item.aiScore} (confidence: {item.aiConfidence})</p>
                            <p className="flag-reason">⚠️ {item.flagReason}</p>
                        </div>
                    </div>
                ))}
            </div>
            
            {selectedItem && (
                <div className="review-panel">
                    <h2>{selectedItem.studentName}</h2>
                    <p>AI Score: {selectedItem.aiScore}</p>
                    <p>AI Confidence: {(selectedItem.aiConfidence * 100).toFixed(0)}%</p>
                    <p>Flag Reason: {selectedItem.flagReason}</p>
                    
                    <iframe
                        title="exam-paper"
                        src={selectedItem.paperUrl}
                        style={{ width: '100%', height: '600px' }}
                    />
                    
                    <div className="review-input">
                        <label>Your Grade:</label>
                        <input
                            type="number"
                            value={reviewScore}
                            onChange={(e) => setReviewScore(parseFloat(e.target.value))}
                            min="0"
                            max="100"
                        />
                        
                        <label>Notes:</label>
                        <textarea
                            value={reviewNotes}
                            onChange={(e) => setReviewNotes(e.target.value)}
                            placeholder="Why did you approve/adjust this grade?"
                        />
                        
                        <button onClick={handleSubmitReview}>Submit Review</button>
                    </div>
                </div>
            )}
        </div>
    );
}
```

---

## Complete Checklist

- [ ] Multi-tenant SSO (Microsoft, Google, Okta)
- [ ] Per-university login branding
- [ ] 16-char complex password generation
- [ ] Mandatory 2FA on first login
- [ ] TOTP setup (Google Authenticator, Microsoft Authenticator)
- [ ] Backup codes for 2FA recovery
- [ ] 2FA verification on every login
- [ ] Password change enforcement
- [ ] Paper scanning pipeline (QR codes, OCR)
- [ ] Auto-split PDFs by student
- [ ] Page count reconciliation
- [ ] Blur + blank detection
- [ ] OCR confidence tracking
- [ ] Agentic grading (specialized agents per problem)
- [ ] Human review queue + dashboard
- [ ] Destruction manifest + audit trail
- [ ] Incineration logging
- [ ] Digital archive + backup

---

**This is enterprise exam management: Secure, auditable, AI-powered, human-reviewed.**

500 universities. Millions of exam papers. All digitized, graded, archived. Zero physical copies. Full audit trail. 🚀
