namespace AegisCore.Infrastructure.Config;

public static class DefaultTenantConfigTemplate
{
    public const string DefaultUniversityName = "Sample University";
    public const string DefaultUniversityShortName = "SU";
    public const string DefaultNoReplyEmail = "noreply@university.edu";
    public const string DefaultSupportEmail = "support@university.edu";
    public const string DefaultFromAddress = "noreply@university.edu";
    public const string DefaultFromName = "AegisCore";
    public const string DefaultSmtpHost = "localhost";
    public const string DefaultSmtpPort = "587";
    public const bool DefaultSslEnabled = true;
    public const string DefaultDatabaseName = "aegiscore";
    public const string DefaultPostgresConnectionString = "Host=localhost;Database=aegiscore";
    public const string DefaultElasticsearchUrl = "http://localhost:9200";
    public const string DefaultSearchIndexPrefix = "aegiscore";
    public const string DefaultRedisConnection = "localhost:6379";
    public const string DefaultSamlBaseUrl = "https://api.university.edu";
    public const string DefaultRaastBaseUrl = "https://api.raast.gov.pk/v1/";
    public const string DefaultOpenAiBaseUrl = "https://api.openai.com/v1/";
    public const string DefaultOllamaBaseUrl = "http://localhost:11434/";
    public const string DefaultOpenAiModel = "gpt-4o-mini";
    public const string DefaultOllamaModel = "llama3.2";
    public const string DefaultEmbeddingModel = "text-embedding-3-small";
    public const string DefaultOllamaEmbeddingModel = "nomic-embed-text";
    public const string DefaultAiProvider = "openai";
    public const decimal MinCGPA = 2.0m;
    public const decimal AcademicProbationCgpa = 2.0m;
    public const decimal ExpulsionCgpaThreshold = 1.7m;
    public const decimal MinAttendancePercentage = 75m;
    public const decimal MinPassingMarks = 50m;
    public const int MaxCreditHoursPerSemester = 21;
    public const int DefaultRateLimitPerMinute = 60;
    public const int DefaultTimeoutSeconds = 60;
    public const int DefaultRetryAfterSeconds = 60;
    public const int DefaultRateLimitBurst = 10;
    public const int BackupRetentionDaysHot = 30;
    public const int BackupRetentionDaysCold = 365;
    public const string DefaultTimezone = "Asia/Karachi";
    public const string DefaultCurrency = "PKR";
    public const string DefaultLanguage = "en-PK";
    public const string DefaultPaymentGateway = "stripe";
    public const string DefaultAccreditorName = "Accreditor";
    public const string DefaultIdentityDocumentName = "Identity document";
    public const int DefaultAuditRetentionYears = 6;
    public const decimal LateFeePercentage = 0.05m;
    public const decimal LibraryFinePerDay = 50m;
    public const int DefaultWaitSeconds = 60;
    public const decimal ScholarshipMeritCgpa = 3.0m;

    public static class FeeDefaults
    {
        public const decimal LateRegistrationFee = 500m;
        public const decimal RecheckingFeePerPaper = 1000m;
        public const decimal TranscriptFee = 500m;
        public const decimal DuplicateDegreeFee = 2000m;
        public const decimal MigrationCertificateFee = 1500m;
        public const decimal AdmissionProcessingFee = 2000m;
    }

    public static class HostelDefaults
    {
        public const decimal HostelFeePerSemester = 12500m;
        public const decimal HostelSecurityDeposit = 5000m;
        public const decimal MessFeePerMonth = 6000m;
    }

    public static class TransportDefaults
    {
        public const decimal TransportFeePerSemester = 5000m;
    }
}
