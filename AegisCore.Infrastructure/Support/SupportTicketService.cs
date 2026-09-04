using System.Text;
using AegisCore.Application.AI;
using AegisCore.Application.Support;
using AegisCore.Infrastructure.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AegisCore.Infrastructure.Support;

public sealed class SupportTicketService
{
    private static readonly SemaphoreSlim SchemaGate = new(1, 1);
    private static bool _schemaReady;
    private static readonly string[] CriticalTerms =
    [
        "data breach", "hacked", "fraud", "harassment", "threat", "suicide",
        "charged twice", "wrong grade", "grade changed", "legal", "emergency"
    ];

    private readonly string _connectionString;
    private readonly LlmProviderFactory _llmFactory;
    private readonly ILogger<SupportTicketService> _logger;

    public SupportTicketService(
        IConfiguration configuration,
        LlmProviderFactory llmFactory,
        ILogger<SupportTicketService> logger)
    {
        _connectionString = ResolveConnectionString(configuration);
        _llmFactory = llmFactory;
        _logger = logger;
    }

    public async Task<SupportTicketDto> CreateAsync(
        Guid tenantId,
        Guid requesterId,
        CreateSupportTicketRequest request,
        CancellationToken ct)
    {
        ValidateRequest(request);
        await EnsureSchemaAsync(ct);

        var ticketId = Guid.NewGuid();
        var category = NormalizeCategory(request.Category);
        var priority = NormalizePriority(request.Priority);
        var combined = $"{request.Title}\n{request.Description}";
        var criticalReason = CriticalTerms.FirstOrDefault(x => combined.Contains(x, StringComparison.OrdinalIgnoreCase));
        var articles = await SearchKnowledgeBaseAsync(tenantId, category, combined, ct);

        string? aiResponse = null;
        decimal confidence = 0;
        var escalationReason = priority == "critical"
            ? "Ticket marked critical by requester"
            : criticalReason is null ? null : $"Critical issue detected: {criticalReason}";

        if (escalationReason is null)
        {
            try
            {
                var provider = _llmFactory.GetLlmProvider(tenantId);
                var context = articles.Count == 0
                    ? "No approved knowledge-base article matched this request."
                    : string.Join("\n\n", articles.Select(x => $"Q: {x.Question}\nA: {x.Answer}"));
                var systemPrompt = """
                    You are the first-line support assistant for a university ERP.
                    Give safe, concrete steps in no more than five short sentences.
                    Use only the approved knowledge-base context when institutional facts are required.
                    Never claim that an email, payment, grade, account, or database change was performed unless a tool result explicitly confirms it.
                    For grades, money, privacy, safety, disciplinary, or account-security decisions, say that a human must verify the record.
                    If the answer is not supported, respond exactly: I need to escalate this to the support team.
                    """;
                aiResponse = await provider.CompleteAsync(
                    systemPrompt,
                    $"Category: {category}\nTicket: {combined}\n\nApproved context:\n{context}",
                    new LlmOptions { MaxTokens = 320, Temperature = 0.2f },
                    ct);
                confidence = EvaluateConfidence(category, aiResponse, articles.Count > 0);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "AI support provider failed for ticket {TicketId}; escalating safely", ticketId);
                aiResponse = "I could not verify a safe answer, so I have sent this request to the support team.";
                confidence = 0;
            }

            if (confidence < 0.70m)
                escalationReason = $"AI confidence below threshold: {confidence:P0}";
        }

        var status = escalationReason is null ? SupportTicketStatuses.Open : SupportTicketStatuses.Escalated;
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        const string insertTicket = """
            INSERT INTO support_tickets
                (id, tenant_id, requester_id, title, description, category, priority, status,
                 ai_response, ai_confidence, escalated_to_human, created_at)
            VALUES
                (@id, @tenant, @requester, @title, @description, @category, @priority, @status,
                 @response, @confidence, @escalated, NOW());
            """;
        await using (var command = new NpgsqlCommand(insertTicket, connection, transaction))
        {
            command.Parameters.AddWithValue("id", ticketId);
            command.Parameters.AddWithValue("tenant", tenantId);
            command.Parameters.AddWithValue("requester", requesterId);
            command.Parameters.AddWithValue("title", request.Title.Trim());
            command.Parameters.AddWithValue("description", request.Description.Trim());
            command.Parameters.AddWithValue("category", category);
            command.Parameters.AddWithValue("priority", priority);
            command.Parameters.AddWithValue("status", status);
            command.Parameters.AddWithValue("response", (object?)aiResponse ?? DBNull.Value);
            command.Parameters.AddWithValue("confidence", confidence);
            command.Parameters.AddWithValue("escalated", escalationReason is not null);
            await command.ExecuteNonQueryAsync(ct);
        }

        if (escalationReason is not null)
            await InsertEscalationAsync(connection, transaction, ticketId, tenantId, escalationReason, ct);

        await transaction.CommitAsync(ct);
        return (await GetAsync(tenantId, requesterId, ticketId, allowAll: false, ct))!;
    }

    public async Task<IReadOnlyList<SupportTicketDto>> ListAsync(
        Guid tenantId,
        Guid requesterId,
        bool allowAll,
        CancellationToken ct)
    {
        await EnsureSchemaAsync(ct);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        const string sql = """
            SELECT id, tenant_id, requester_id, title, description, category, priority, status,
                   ai_response, ai_confidence, customer_rating, customer_feedback, human_response,
                   created_at, resolved_at
              FROM support_tickets
             WHERE tenant_id = @tenant
               AND (@allow_all OR requester_id = @requester)
             ORDER BY created_at DESC LIMIT 250;
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("allow_all", allowAll);
        command.Parameters.AddWithValue("requester", requesterId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var tickets = new List<SupportTicketDto>();
        while (await reader.ReadAsync(ct)) tickets.Add(ReadTicket(reader));
        return tickets;
    }

    public async Task<SupportTicketDto?> GetAsync(
        Guid tenantId,
        Guid requesterId,
        Guid ticketId,
        bool allowAll,
        CancellationToken ct)
    {
        await EnsureSchemaAsync(ct);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        const string sql = """
            SELECT id, tenant_id, requester_id, title, description, category, priority, status,
                   ai_response, ai_confidence, customer_rating, customer_feedback, human_response,
                   created_at, resolved_at
              FROM support_tickets
             WHERE tenant_id = @tenant AND id = @id
               AND (@allow_all OR requester_id = @requester);
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("id", ticketId);
        command.Parameters.AddWithValue("allow_all", allowAll);
        command.Parameters.AddWithValue("requester", requesterId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadTicket(reader) : null;
    }

    public async Task<SupportTicketDto?> RecordFeedbackAsync(
        Guid tenantId,
        Guid requesterId,
        Guid ticketId,
        SupportFeedbackRequest request,
        CancellationToken ct)
    {
        if (request.Rating is < 1 or > 5) throw new ArgumentException("Rating must be between 1 and 5.");
        await EnsureSchemaAsync(ct);
        var ticket = await GetAsync(tenantId, requesterId, ticketId, false, ct);
        if (ticket is null) return null;

        var status = request.Rating >= 4 ? SupportTicketStatuses.AiResolved :
            request.Rating <= 2 ? SupportTicketStatuses.Escalated : SupportTicketStatuses.Open;
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        const string sql = """
            UPDATE support_tickets
               SET customer_rating = @rating,
                   customer_feedback = @feedback,
                   csat_score = (@rating - 1) * 25,
                   status = @status,
                   escalated_to_human = CASE WHEN @rating <= 2 THEN TRUE ELSE escalated_to_human END,
                   resolved_at = CASE WHEN @rating >= 4 THEN NOW() ELSE resolved_at END
             WHERE id = @id AND tenant_id = @tenant AND requester_id = @requester;
            """;
        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("rating", request.Rating);
            command.Parameters.AddWithValue("feedback", (object?)request.Feedback?.Trim() ?? DBNull.Value);
            command.Parameters.AddWithValue("status", status);
            command.Parameters.AddWithValue("id", ticketId);
            command.Parameters.AddWithValue("tenant", tenantId);
            command.Parameters.AddWithValue("requester", requesterId);
            await command.ExecuteNonQueryAsync(ct);
        }
        if (request.Rating <= 2)
            await InsertEscalationAsync(connection, transaction, ticketId, tenantId, "Customer rejected the AI response", ct);
        await transaction.CommitAsync(ct);
        return await GetAsync(tenantId, requesterId, ticketId, false, ct);
    }

    public async Task<IReadOnlyList<SupportEscalationDto>> ListEscalationsAsync(Guid tenantId, CancellationToken ct)
    {
        await EnsureSchemaAsync(ct);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        const string sql = """
            SELECT e.id, e.ticket_id, e.reason, e.status, e.created_at,
                   t.id, t.tenant_id, t.requester_id, t.title, t.description, t.category, t.priority, t.status,
                   t.ai_response, t.ai_confidence, t.customer_rating, t.customer_feedback, t.human_response,
                   t.created_at, t.resolved_at
              FROM support_escalations e
              JOIN support_tickets t ON t.id = e.ticket_id AND t.tenant_id = e.tenant_id
             WHERE e.tenant_id = @tenant AND e.status = 'pending'
             ORDER BY e.created_at ASC;
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<SupportEscalationDto>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new SupportEscalationDto(
                reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
                ReadTimestamp(reader, 4), ReadTicket(reader, 5)));
        }
        return result;
    }

    public async Task<bool> ResolveEscalationAsync(
        Guid tenantId,
        Guid escalationId,
        Guid resolvedBy,
        ResolveEscalationRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Response)) throw new ArgumentException("A response is required.");
        await EnsureSchemaAsync(ct);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        const string select = """
            SELECT e.ticket_id, t.category, t.description
              FROM support_escalations e JOIN support_tickets t ON t.id = e.ticket_id AND t.tenant_id = e.tenant_id
             WHERE e.id = @id AND e.tenant_id = @tenant AND e.status = 'pending'
             FOR UPDATE;
            """;
        Guid ticketId;
        string category;
        string question;
        await using (var command = new NpgsqlCommand(select, connection, transaction))
        {
            command.Parameters.AddWithValue("id", escalationId);
            command.Parameters.AddWithValue("tenant", tenantId);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return false;
            ticketId = reader.GetGuid(0);
            category = reader.GetString(1);
            question = reader.GetString(2);
        }

        const string updateTicket = """
            UPDATE support_tickets
               SET status = 'closed', human_response = @response, human_resolved_at = NOW(),
                   human_resolved_by = @resolvedBy, resolved_at = NOW()
             WHERE id = @ticket AND tenant_id = @tenant;
            UPDATE support_escalations
               SET status = 'resolved', resolved_at = NOW(), resolved_by = @resolvedBy
             WHERE id = @id AND tenant_id = @tenant;
            """;
        await using (var command = new NpgsqlCommand(updateTicket, connection, transaction))
        {
            command.Parameters.AddWithValue("response", request.Response.Trim());
            command.Parameters.AddWithValue("resolvedBy", resolvedBy);
            command.Parameters.AddWithValue("ticket", ticketId);
            command.Parameters.AddWithValue("id", escalationId);
            command.Parameters.AddWithValue("tenant", tenantId);
            await command.ExecuteNonQueryAsync(ct);
        }

        if (request.AddToKnowledgeBase)
            await InsertKnowledgeAsync(connection, transaction, tenantId, category, question, request.Response.Trim(), "from_ticket", ticketId, resolvedBy, ct);

        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<Guid> AddKnowledgeAsync(
        Guid tenantId,
        Guid createdBy,
        KnowledgeArticleRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question) || string.IsNullOrWhiteSpace(request.Answer))
            throw new ArgumentException("Question and answer are required.");
        await EnsureSchemaAsync(ct);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var id = await InsertKnowledgeAsync(connection, transaction, tenantId, NormalizeCategory(request.Category),
            request.Question.Trim(), request.Answer.Trim(), "manual_entry", null, createdBy, ct);
        await transaction.CommitAsync(ct);
        return id;
    }

    public async Task<SupportAnalytics> GetAnalyticsAsync(Guid tenantId, CancellationToken ct)
    {
        await EnsureSchemaAsync(ct);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        const string sql = """
            SELECT COUNT(*)::int,
                   COALESCE(AVG(csat_score), 0)::numeric,
                   COALESCE(100.0 * COUNT(*) FILTER (WHERE status = 'ai_resolved') / NULLIF(COUNT(*), 0), 0)::numeric,
                   COALESCE(100.0 * COUNT(*) FILTER (WHERE escalated_to_human) / NULLIF(COUNT(*), 0), 0)::numeric,
                   COALESCE(AVG(EXTRACT(EPOCH FROM (resolved_at - created_at)) / 3600.0)
                       FILTER (WHERE resolved_at IS NOT NULL), 0)::numeric
              FROM support_tickets WHERE tenant_id = @tenant;
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return new SupportAnalytics(
            reader.GetInt32(0),
            decimal.Round(reader.GetDecimal(1), 1),
            decimal.Round(reader.GetDecimal(2), 1),
            decimal.Round(reader.GetDecimal(3), 1),
            decimal.Round(reader.GetDecimal(4), 2));
    }

    public static decimal EvaluateConfidence(string category, string response, bool hasKnowledge)
    {
        if (string.IsNullOrWhiteSpace(response)) return 0;
        var confidence = category switch
        {
            "password_reset" => 0.82m,
            "attendance" => 0.78m,
            "grades" => 0.62m,
            "fees" => 0.58m,
            _ => 0.55m
        };
        if (hasKnowledge) confidence += 0.12m;
        var uncertain = new[] { "might", "probably", "not sure", "don't know", "need to escalate", "cannot verify" };
        if (uncertain.Any(x => response.Contains(x, StringComparison.OrdinalIgnoreCase))) confidence -= 0.35m;
        if (response.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length is >= 12 and <= 120) confidence += 0.04m;
        return Math.Clamp(confidence, 0, 1);
    }

    private async Task<List<KnowledgeMatch>> SearchKnowledgeBaseAsync(
        Guid tenantId,
        string category,
        string query,
        CancellationToken ct)
    {
        var terms = query.Split(new[] { ' ', '\r', '\n', '\t', '.', ',', '?', '!', ':', ';' },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.Length >= 4)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
        if (terms.Length == 0) return [];

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        const string sql = """
            SELECT question, answer,
                   (CASE WHEN question ILIKE @q0 OR answer ILIKE @q0 THEN 1 ELSE 0 END
                  + CASE WHEN question ILIKE @q1 OR answer ILIKE @q1 THEN 1 ELSE 0 END
                  + CASE WHEN question ILIKE @q2 OR answer ILIKE @q2 THEN 1 ELSE 0 END
                  + CASE WHEN question ILIKE @q3 OR answer ILIKE @q3 THEN 1 ELSE 0 END
                  + CASE WHEN question ILIKE @q4 OR answer ILIKE @q4 THEN 1 ELSE 0 END
                  + CASE WHEN question ILIKE @q5 OR answer ILIKE @q5 THEN 1 ELSE 0 END
                  + CASE WHEN question ILIKE @q6 OR answer ILIKE @q6 THEN 1 ELSE 0 END
                  + CASE WHEN question ILIKE @q7 OR answer ILIKE @q7 THEN 1 ELSE 0 END) AS score
              FROM support_knowledge_base
             WHERE tenant_id = @tenant AND is_active = TRUE
               AND (category = @category OR category = 'other')
               AND (question ILIKE @q0 OR answer ILIKE @q0
                 OR question ILIKE @q1 OR answer ILIKE @q1
                 OR question ILIKE @q2 OR answer ILIKE @q2
                 OR question ILIKE @q3 OR answer ILIKE @q3
                 OR question ILIKE @q4 OR answer ILIKE @q4
                 OR question ILIKE @q5 OR answer ILIKE @q5
                 OR question ILIKE @q6 OR answer ILIKE @q6
                 OR question ILIKE @q7 OR answer ILIKE @q7)
             ORDER BY score DESC, helpful_count DESC, created_at DESC
             LIMIT 5;
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("category", category);
        for (var i = 0; i < 8; i++) command.Parameters.AddWithValue($"q{i}", i < terms.Length ? $"%{terms[i]}%" : "\u0001");
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<KnowledgeMatch>();
        while (await reader.ReadAsync(ct)) result.Add(new(reader.GetString(0), reader.GetString(1)));
        return result;
    }

    private async Task EnsureSchemaAsync(CancellationToken ct)
    {
        if (_schemaReady) return;
        await SchemaGate.WaitAsync(ct);
        try
        {
            if (_schemaReady) return;
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            const string sql = """
                CREATE TABLE IF NOT EXISTS support_tickets (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    requester_id uuid NOT NULL,
                    title varchar(255) NOT NULL,
                    description text NOT NULL,
                    category varchar(50) NOT NULL,
                    priority varchar(20) NOT NULL DEFAULT 'normal',
                    status varchar(30) NOT NULL,
                    ai_response text NULL,
                    ai_confidence numeric(3,2) NULL,
                    customer_rating int NULL CHECK (customer_rating BETWEEN 1 AND 5),
                    customer_feedback text NULL,
                    csat_score int NULL CHECK (csat_score BETWEEN 0 AND 100),
                    escalated_to_human boolean NOT NULL DEFAULT false,
                    human_response text NULL,
                    human_resolved_by uuid NULL,
                    human_resolved_at timestamptz NULL,
                    created_at timestamptz NOT NULL DEFAULT NOW(),
                    resolved_at timestamptz NULL
                );
                CREATE INDEX IF NOT EXISTS ix_support_tickets_tenant_requester
                    ON support_tickets (tenant_id, requester_id, created_at DESC);
                CREATE INDEX IF NOT EXISTS ix_support_tickets_queue
                    ON support_tickets (tenant_id, status, priority, created_at);

                CREATE TABLE IF NOT EXISTS support_knowledge_base (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    category varchar(50) NOT NULL,
                    question text NOT NULL,
                    answer text NOT NULL,
                    source varchar(30) NOT NULL,
                    ticket_id uuid NULL REFERENCES support_tickets(id) ON DELETE SET NULL,
                    created_by uuid NOT NULL,
                    helpful_count int NOT NULL DEFAULT 0,
                    is_active boolean NOT NULL DEFAULT true,
                    created_at timestamptz NOT NULL DEFAULT NOW()
                );
                CREATE INDEX IF NOT EXISTS ix_support_kb_tenant_category
                    ON support_knowledge_base (tenant_id, category, created_at DESC);

                CREATE TABLE IF NOT EXISTS support_escalations (
                    id uuid PRIMARY KEY,
                    ticket_id uuid NOT NULL REFERENCES support_tickets(id) ON DELETE CASCADE,
                    tenant_id uuid NOT NULL,
                    reason varchar(300) NOT NULL,
                    status varchar(20) NOT NULL DEFAULT 'pending',
                    resolved_by uuid NULL,
                    resolved_at timestamptz NULL,
                    created_at timestamptz NOT NULL DEFAULT NOW()
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ux_support_pending_escalation
                    ON support_escalations (ticket_id) WHERE status = 'pending';
                CREATE INDEX IF NOT EXISTS ix_support_escalations_queue
                    ON support_escalations (tenant_id, status, created_at);
                """;
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(ct);
            _schemaReady = true;
        }
        finally { SchemaGate.Release(); }
    }

    private static async Task InsertEscalationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid ticketId,
        Guid tenantId,
        string reason,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO support_escalations (id, ticket_id, tenant_id, reason, status, created_at)
            VALUES (@id, @ticket, @tenant, @reason, 'pending', NOW())
            ON CONFLICT (ticket_id) WHERE status = 'pending'
            DO UPDATE SET reason = EXCLUDED.reason;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("ticket", ticketId);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("reason", reason);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<Guid> InsertKnowledgeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        string category,
        string question,
        string answer,
        string source,
        Guid? ticketId,
        Guid createdBy,
        CancellationToken ct)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO support_knowledge_base
                (id, tenant_id, category, question, answer, source, ticket_id, created_by, created_at)
            VALUES (@id, @tenant, @category, @question, @answer, @source, @ticket, @createdBy, NOW());
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("category", category);
        command.Parameters.AddWithValue("question", question);
        command.Parameters.AddWithValue("answer", answer);
        command.Parameters.AddWithValue("source", source);
        command.Parameters.AddWithValue("ticket", (object?)ticketId ?? DBNull.Value);
        command.Parameters.AddWithValue("createdBy", createdBy);
        await command.ExecuteNonQueryAsync(ct);
        return id;
    }

    private static SupportTicketDto ReadTicket(NpgsqlDataReader reader, int offset = 0) =>
        new(
            reader.GetGuid(offset), reader.GetGuid(offset + 1), reader.GetGuid(offset + 2),
            reader.GetString(offset + 3), reader.GetString(offset + 4), reader.GetString(offset + 5),
            reader.GetString(offset + 6), reader.GetString(offset + 7),
            reader.IsDBNull(offset + 8) ? null : reader.GetString(offset + 8),
            reader.IsDBNull(offset + 9) ? null : reader.GetDecimal(offset + 9),
            reader.IsDBNull(offset + 10) ? null : reader.GetInt32(offset + 10),
            reader.IsDBNull(offset + 11) ? null : reader.GetString(offset + 11),
            reader.IsDBNull(offset + 12) ? null : reader.GetString(offset + 12),
            ReadTimestamp(reader, offset + 13),
            reader.IsDBNull(offset + 14) ? null : ReadTimestamp(reader, offset + 14));

    private static DateTimeOffset ReadTimestamp(NpgsqlDataReader reader, int ordinal)
    {
        var value = reader.GetDateTime(ordinal);
        return value.Kind == DateTimeKind.Utc
            ? new DateTimeOffset(value)
            : new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private static void ValidateRequest(CreateSupportTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length is < 4 or > 255)
            throw new ArgumentException("Title must be between 4 and 255 characters.");
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length is < 10 or > 10_000)
            throw new ArgumentException("Description must be between 10 and 10,000 characters.");
    }

    private static string NormalizeCategory(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "password_reset" or "grades" or "fees" or "attendance" => value.Trim().ToLowerInvariant(),
            _ => "other"
        };

    private static string NormalizePriority(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "low" or "normal" or "high" or "critical" => value.Trim().ToLowerInvariant(),
            _ => "normal"
        };

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var explicitValue = configuration.GetConnectionString("Default")
            ?? configuration["ConnectionStrings:Default"]
            ?? configuration["Database:ConnectionString"]
            ?? configuration["AEGISCORE_DB_CONNECTION"]
            ?? configuration["POSTGRES_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(explicitValue)) return explicitValue;
        var host = configuration["DB_HOST"] ?? configuration["POSTGRES_HOST"] ?? "localhost";
        var port = configuration["DB_PORT"] ?? configuration["POSTGRES_PORT"] ?? "5432";
        var database = configuration["DB_NAME"] ?? configuration["POSTGRES_DB"] ?? "aegiscore";
        var username = configuration["DB_USER"] ?? configuration["POSTGRES_USER"] ?? "aegiscore";
        var password = configuration["DB_PASS"] ?? configuration["POSTGRES_PASSWORD"] ?? "";
        var ssl = (configuration["DB_SSL"] ?? "Require").Equals("Disable", StringComparison.OrdinalIgnoreCase) ? "Disable" : "Require";
        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SslMode={ssl}";
    }

    private sealed record KnowledgeMatch(string Question, string Answer);
}
