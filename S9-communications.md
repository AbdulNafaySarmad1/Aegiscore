# Nocturne — Sprint 9: Communications Hub (WhatsApp + Email + SMS + Notifications)

## Role
You are a senior .NET 10 / C# 14 engineer building the unified communications engine for Nocturne. WhatsApp achieves 95%+ open rates; email is under 20%. We're using both strategically.

---

## Task 1 — WhatsApp Business API Integration

### Setup
- Meta WhatsApp Business API (requires business account verification)
- Webhook for incoming messages
- Template management for outgoing messages

### Database Tables
```sql
CREATE TABLE whatsapp_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_name VARCHAR(100) NOT NULL UNIQUE,
    template_body TEXT NOT NULL,
    template_category VARCHAR(50), -- marketing | transactional | otp
    variables JSONB, -- {{{1}}, {{2}}, etc}
    status VARCHAR(20) DEFAULT 'pending', -- pending | approved | rejected
    meta_template_id VARCHAR(200),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE communication_triggers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type VARCHAR(50) NOT NULL, -- fee_due | attendance_alert | result_published | etc
    whatsapp_template_id UUID REFERENCES whatsapp_templates(id),
    email_template_id UUID REFERENCES email_templates(id),
    sms_template_id UUID REFERENCES sms_templates(id),
    is_enabled BOOLEAN DEFAULT TRUE,
    send_via VARCHAR(50) DEFAULT 'whatsapp', -- whatsapp | email | sms | all
    recipient_roles VARCHAR(200), -- comma-separated roles
    days_before_event INT, -- e.g., send 3 days before fee due
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE communication_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    trigger_id UUID NOT NULL REFERENCES communication_triggers(id),
    recipient_id UUID NOT NULL REFERENCES users(id),
    recipient_phone VARCHAR(20),
    recipient_email VARCHAR(200),
    template_used VARCHAR(100),
    channel VARCHAR(50), -- whatsapp | email | sms
    status VARCHAR(20) DEFAULT 'pending', -- pending | sent | failed | delivered | read
    meta_message_id VARCHAR(200),
    sent_at TIMESTAMPTZ,
    delivered_at TIMESTAMPTZ,
    read_at TIMESTAMPTZ,
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### WhatsApp Templates (Pre-approved by Meta)

1. **Fee Reminder**
   ```
   Assalam-o-alaikum {{name}}!
   
   Your semester fee of {{amount}} is due on {{due_date}}.
   
   Pay here: {{payment_link}}
   ```

2. **Attendance Alert**
   ```
   {{name}}, your attendance in {{course_code}} is {{attendance}}%.
   Minimum required: 75%.
   Contact your faculty if you have valid reasons for absences.
   ```

3. **Result Published**
   ```
   {{name}}, results for {{semester}} have been published!
   Your SGPA: {{sgpa}}
   View results: {{portal_link}}
   ```

4. **Hall Ticket Ready**
   ```
   {{name}}, your hall ticket for {{course_code}} exam on {{exam_date}} is ready.
   Download: {{portal_link}}
   ```

5. **Grievance Status Update**
   ```
   {{name}}, your grievance #{{ticket_id}} status: {{status}}.
   Response: {{message}}
   ```

### WhatsApp Service
```csharp
public interface IWhatsAppService
{
    Task<bool> SendTemplateAsync(Guid userId, string templateName, Dictionary<string, string> variables, CancellationToken ct = default);
    Task<bool> SendBulkAsync(List<Guid> userIds, string templateName, Dictionary<string, string> variables, CancellationToken ct = default);
    Task<WebhookResult> HandleIncomingMessageAsync(WebhookRequest request, CancellationToken ct = default);
}

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    
    public async Task<bool> SendTemplateAsync(
        Guid userId, string templateName, Dictionary<string, string> variables, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(userId);
        var template = await _db.WhatsAppTemplates.FirstAsync(x => x.TemplateName == templateName, ct);
        
        var request = new
        {
            messaging_product = "whatsapp",
            to = user.PhoneNumber, // E.164 format: +923001234567
            type = "template",
            template = new
            {
                name = template.MetaTemplateId,
                language = new { code = "ur" }, // Urdu
                parameters = new
                {
                    body = new
                    {
                        parameters = variables.Values.ToList()
                    }
                }
            }
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            $"https://graph.instagram.com/v18.0/{_config["WhatsApp:PhoneNumberId"]}/messages",
            request,
            ct
        );
        
        var result = await response.Content.ReadAsAsync<dynamic>(ct);
        
        var logEntry = new CommunicationLog
        {
            RecipientId = userId,
            RecipientPhone = user.PhoneNumber,
            TemplateName = templateName,
            Channel = "whatsapp",
            Status = response.IsSuccessStatusCode ? "sent" : "failed",
            MetaMessageId = result.messages[0].id,
            SentAt = DateTime.UtcNow
        };
        
        _db.CommunicationLogs.Add(logEntry);
        await _db.SaveChangesAsync(ct);
        
        return response.IsSuccessStatusCode;
    }
}
```

### Webhook for Delivery Status
```csharp
[HttpPost("/webhook/whatsapp")]
public async Task<IActionResult> WhatsAppWebhookAsync([FromBody] WebhookPayload payload)
{
    foreach (var change in payload.Entry[0].Changes)
    {
        if (change.Field == "messages")
        {
            foreach (var message in change.Value.Messages)
            {
                // Update delivery status in communication_logs
                var log = await _db.CommunicationLogs
                    .FirstAsync(x => x.MetaMessageId == message.Id);
                
                log.Status = message.Status.ToString(); // delivered | read | failed
                log.DeliveredAt = DateTime.UtcNow;
                
                await _db.SaveChangesAsync();
            }
        }
    }
    
    return Ok();
}
```

---

## Task 2 — Email & SMS Templates

### Email Templates
```sql
CREATE TABLE email_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_name VARCHAR(100) NOT NULL UNIQUE,
    subject_line TEXT NOT NULL,
    html_body TEXT NOT NULL,
    variables JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

Common templates:
- Fee reminder
- Admission confirmation
- Hall ticket issued
- Result published
- Transcript request approved
- Leave approved/rejected
- Grievance update

### SMS Templates (for no-internet areas)
```sql
CREATE TABLE sms_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_name VARCHAR(100) NOT NULL UNIQUE,
    message_text VARCHAR(160) NOT NULL, -- SMS character limit
    variables JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Bulk Communication
Admin can send bulk notifications to groups:
- All students in program
- All students with fee overdue
- All students with low attendance
- All faculty in department
- Specific cohorts

---

## Task 3 — In-App Notification Center

### Database Table
```sql
CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    notification_type VARCHAR(50), -- fee | attendance | result | grievance | announcement
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    is_read BOOLEAN DEFAULT FALSE,
    link_url VARCHAR(500), -- e.g., /portal/fees or /admin/grievance/123
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    read_at TIMESTAMPTZ
);
```

### Real-Time Push (SignalR)
```csharp
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        await base.OnConnectedAsync();
    }
}

public class NotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    
    public async Task SendNotificationAsync(Guid userId, NotificationDto notification)
    {
        // Save to DB
        var entity = new Notification { UserId = userId, ...notification };
        _db.Notifications.Add(entity);
        await _db.SaveChangesAsync();
        
        // Send real-time push
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("ReceiveNotification", notification);
    }
}
```

---

## Task 4 — Notice Board & Circular Management

### Database Table
```sql
CREATE TABLE notice_board_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    posted_by UUID NOT NULL REFERENCES users(id),
    visibility VARCHAR(50) NOT NULL, -- public | students | faculty | staff | all
    posted_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expiry_date TIMESTAMPTZ,
    is_archived BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## Task 5 — Grievance Ticketing System

### Database Table
```sql
CREATE TABLE grievance_tickets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_number VARCHAR(50) NOT NULL UNIQUE,
    submitter_id UUID NOT NULL REFERENCES users(id),
    category VARCHAR(50) NOT NULL, -- academic | financial | conduct | other
    subject VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    status VARCHAR(20) DEFAULT 'open', -- open | acknowledged | in_progress | resolved | closed
    assigned_to UUID REFERENCES users(id),
    priority VARCHAR(20) DEFAULT 'normal', -- low | normal | high | urgent
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at TIMESTAMPTZ,
    resolution_notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE grievance_responses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES grievance_tickets(id),
    responder_id UUID NOT NULL REFERENCES users(id),
    response_text TEXT NOT NULL,
    status_update VARCHAR(20),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## Deliverables Checklist
- [ ] WhatsApp Business API integration (send + receive)
- [ ] Template management (WhatsApp, Email, SMS)
- [ ] Communication trigger setup per event
- [ ] Bulk communication for groups
- [ ] In-app notification center (with real-time SignalR)
- [ ] Notice board with expiry management
- [ ] Grievance ticketing system
- [ ] Communication log + delivery status tracking
- [ ] Email service (SMTP integration)
- [ ] SMS service (for fallback)
- [ ] Notification preferences (user can opt-out)
- [ ] All tests
