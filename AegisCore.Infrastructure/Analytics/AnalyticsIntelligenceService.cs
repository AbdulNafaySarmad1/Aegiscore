using System.Security.Cryptography;
using System.Text.Json;
using AegisCore.Application.Analytics;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.Analytics;

public sealed class AnalyticsIntelligenceService(
    AegisDbContext db,
    IStatisticalAnalyticsEngine statistics,
    IConfiguration configuration) : IAnalyticsIntelligenceService
{
    private const string ExplanationVersion = "1.0";
    private static readonly HashSet<string> ProhibitedSensitiveFeatures = new(StringComparer.OrdinalIgnoreCase)
    {
        "race", "ethnicity", "religion", "gender", "sex", "disability", "cnic", "national_id", "political_affiliation"
    };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly int _minimumCohortSize = Math.Max(2, configuration.GetValue<int?>("Analytics:MinimumCohortSize") ?? 10);

    public async Task<StudentTelemetry?> GetLatestTelemetryAsync(Guid studentId, Guid? courseId, CancellationToken ct = default)
    {
        db.GetRequiredTenantId();
        return await ReadLatestTelemetryAsync(studentId, courseId, ct);
    }

    public async Task<StudentTelemetry?> GetAuthorizedTelemetryAsync(
        Guid studentId,
        Guid? courseId,
        Guid? departmentScopeId,
        bool tenantWide,
        CancellationToken ct = default)
    {
        var tenantId = db.GetRequiredTenantId();
        var departmentId = await StudentDepartmentQuery(studentId, tenantId).SingleOrDefaultAsync(ct);
        if (departmentId == Guid.Empty) return null;
        if (!tenantWide && (departmentScopeId is null || departmentScopeId == Guid.Empty || departmentScopeId != departmentId))
            throw new UnauthorizedAccessException("The requested student is outside the authorized department scope.");
        return await ReadLatestTelemetryAsync(studentId, courseId, ct);
    }

    public async Task<AnalyticsEnvelope<GradeExplanation>?> GetGradeExplanationAsync(
        Guid courseGradeId,
        Guid studentId,
        CancellationToken ct = default)
    {
        var tenantId = db.GetRequiredTenantId();
        var grade = await (
            from courseGrade in db.CourseGrades.AsNoTracking()
            join student in db.Students.AsNoTracking() on courseGrade.StudentId equals student.Id
            join batch in db.Batches.AsNoTracking() on student.BatchId equals batch.Id
            join studentDepartment in db.Departments.AsNoTracking() on batch.DepartmentId equals studentDepartment.Id
            join course in db.Courses.AsNoTracking() on courseGrade.CourseId equals course.Id
            join courseDepartment in db.Departments.AsNoTracking() on course.DepartmentId equals courseDepartment.Id
            where courseGrade.Id == courseGradeId
                && courseGrade.StudentId == studentId
                && courseGrade.ResultStatus == "published"
                && courseGrade.PublishedAt != null
                && studentDepartment.TenantId == tenantId
                && courseDepartment.TenantId == tenantId
            select courseGrade).SingleOrDefaultAsync(ct);
        if (grade is null) return null;
        var recordedGrade = await ReadLatestDecisionSnapshotAsync<GradeExplanation>("grade", grade.Id, ct);
        if (recordedGrade is not null) return recordedGrade;

        var scheme = await db.AssessmentSchemes.AsNoTracking()
            .Include(item => item.Components)
            .SingleOrDefaultAsync(item => item.CourseId == grade.CourseId && item.SemesterId == grade.SemesterId, ct);
        if (scheme is null)
            return UnavailableGrade("The assessment scheme used for this grade is unavailable.");
        if (scheme.Components.Count == 0 || scheme.Components.Sum(item => item.WeightPercentage) != 100
            || scheme.Components.Any(item => item.TotalMarks <= 0 || item.WeightPercentage < 0))
            return UnavailableGrade("The stored assessment scheme is not arithmetically valid.");

        var componentIds = scheme.Components.Select(item => item.Id).ToHashSet();
        var marks = await db.CourseMarks.AsNoTracking()
            .Where(item => item.CourseId == grade.CourseId && item.StudentId == studentId && componentIds.Contains(item.AssessmentComponentId))
            .ToDictionaryAsync(item => item.AssessmentComponentId, ct);
        if (marks.Count != scheme.Components.Count)
            return UnavailableGrade("Marks for every assessment component are required to reproduce this grade.");

        var components = new List<GradeComponentExplanation>(scheme.Components.Count);
        foreach (var component in scheme.Components.OrderBy(item => item.OrderIndex))
        {
            var mark = marks[component.Id];
            if (mark.ObtainedMarks < 0 || mark.ObtainedMarks > component.TotalMarks)
                return UnavailableGrade("A stored component score is outside its valid bounds.");
            var normalized = (decimal)mark.ObtainedMarks / component.TotalMarks * 100m;
            components.Add(new GradeComponentExplanation(component.Id, component.ComponentType, mark.ObtainedMarks,
                component.TotalMarks, Math.Round(normalized, 4), component.WeightPercentage,
                Math.Round(normalized / 100m * component.WeightPercentage, 4)));
        }

        var weightedTotal = Math.Round(components.Sum(item => item.WeightedContribution), 2, MidpointRounding.AwayFromZero);
        if (weightedTotal != grade.PercentageObtained)
            return UnavailableGrade("The current marks and scheme do not reproduce the persisted final score.");

        var matchingBands = await (
            from band in db.GradeBands.AsNoTracking()
            join scale in db.GradeScales.AsNoTracking() on band.GradeScaleId equals scale.Id
            where scale.UniversityId == tenantId
                && band.LetterGrade == grade.LetterGrade
                && grade.PercentageObtained >= band.PercentageFrom
                && grade.PercentageObtained <= band.PercentageTo
            select band)
            .ToListAsync(ct);
        if (matchingBands.Count != 1)
            return UnavailableGrade("The applied grade boundary is missing or ambiguous across configured grade scales.");
        var appliedBand = matchingBands[0];
        var scaleBands = await db.GradeBands.AsNoTracking()
            .Where(item => item.GradeScaleId == appliedBand.GradeScaleId)
            .OrderBy(item => item.PercentageFrom)
            .ToListAsync(ct);
        var nextBand = scaleBands.FirstOrDefault(item => item.PercentageFrom > grade.PercentageObtained);

        var policyInput = new
        {
            Scheme = new { scheme.Id, scheme.CourseId, scheme.SemesterId, scheme.CreatedAt },
            Components = scheme.Components.OrderBy(item => item.OrderIndex).Select(item => new
            {
                item.Id, item.ComponentType, item.WeightPercentage, item.TotalMarks, item.PassingMarks, item.IsMandatory, item.OrderIndex
            }),
            GradeScaleId = appliedBand.GradeScaleId,
            Bands = scaleBands.Select(item => new { item.Id, item.LetterGrade, item.GpaPoints, item.PercentageFrom, item.PercentageTo })
        };
        var policyVersion = $"sha256:{Hash(policyInput)}";
        var context = await BuildGradeContextAsync(grade, tenantId, ct);
        var calculatedAt = grade.CalculatedAt ?? grade.PublishedAt ?? scheme.CreatedAt;
        var input = new
        {
            grade.Id, grade.CourseId, grade.StudentId, grade.SemesterId, grade.PercentageObtained, grade.LetterGrade,
            Components = components, PolicyVersion = policyVersion, CalculatedAt = calculatedAt
        };
        var inputHash = Hash(input);
        var existing = await ReadDecisionSnapshotAsync<GradeExplanation>("grade", grade.Id, inputHash, ct);
        if (existing is not null) return existing;

        var explanation = new GradeExplanation(
            grade.Id,
            grade.StudentId,
            grade.CourseId,
            grade.SemesterId,
            grade.PercentageObtained,
            grade.LetterGrade ?? "F",
            policyVersion,
            components,
            weightedTotal,
            ToBoundary(appliedBand),
            nextBand is null ? null : ToBoundary(nextBand),
            nextBand is null ? null : Math.Max(0m, nextBand.PercentageFrom - grade.PercentageObtained),
            "Round weighted percentage to 2 decimals using MidpointRounding.AwayFromZero",
            null,
            context,
            calculatedAt,
            DateTimeOffset.UtcNow);
        return await PersistDecisionSnapshotAsync("grade", "CourseGrade", grade.Id, ExplanationSource.Rule,
            policyVersion, calculatedAt, inputHash, explanation, ct);

        AnalyticsEnvelope<GradeExplanation> UnavailableGrade(string reason) => new(
            ExplanationKind.DeterministicDecision,
            ExplanationSource.Rule,
            new Availability(AvailabilityStatus.Unavailable, reason),
            null,
            ExplanationVersion,
            DateTimeOffset.UtcNow);
    }

    public async Task<AnalyticsEnvelope<AdmissionDecisionExplanation>?> GetAdmissionExplanationAsync(
        Guid applicationId,
        CancellationToken ct = default)
    {
        var tenantId = db.GetRequiredTenantId();
        var application = await (
            from item in db.AdmissionApplications.AsNoTracking()
            join program in db.DegreePrograms.AsNoTracking() on item.ProgramId equals program.Id
            where item.Id == applicationId && program.TenantId == tenantId
            select item).SingleOrDefaultAsync(ct);
        if (application is null) return null;
        var recordedDecision = await ReadLatestDecisionSnapshotAsync<AdmissionDecisionExplanation>("admission", application.Id, ct);
        if (recordedDecision is not null) return recordedDecision;

        var entries = await db.MeritListEntries.AsNoTracking()
            .Include(item => item.MeritList)
            .Where(item => item.ApplicationId == applicationId
                && item.MeritList.Status == "published" && item.MeritList.PublishedAt != null)
            .ToListAsync(ct);
        var entry = entries.OrderByDescending(item => item.MeritList.GeneratedAt).FirstOrDefault();
        if (entry is null)
        {
            return new AnalyticsEnvelope<AdmissionDecisionExplanation>(
                ExplanationKind.DeterministicDecision,
                ExplanationSource.Rule,
                new Availability(AvailabilityStatus.Unavailable, "No persisted merit-list decision exists for this application."),
                null,
                ExplanationVersion,
                DateTimeOffset.UtcNow);
        }

        var list = entry.MeritList;
        if (list.ProgramId != application.ProgramId || list.QuotaType != application.QuotaType)
            return new AnalyticsEnvelope<AdmissionDecisionExplanation>(
                ExplanationKind.DeterministicDecision,
                ExplanationSource.Rule,
                new Availability(AvailabilityStatus.Unavailable, "The merit-list entry is inconsistent with the application."),
                null,
                ExplanationVersion,
                DateTimeOffset.UtcNow);

        var expectedAggregate = Math.Round(entry.MatricContribution + entry.FscContribution + entry.EntryTestContribution, 2, MidpointRounding.AwayFromZero);
        if (expectedAggregate != entry.MeritScore)
            return new AnalyticsEnvelope<AdmissionDecisionExplanation>(
                ExplanationKind.DeterministicDecision,
                ExplanationSource.Rule,
                new Availability(AvailabilityStatus.Unavailable, "The persisted merit components do not reproduce the aggregate score."),
                null,
                ExplanationVersion,
                DateTimeOffset.UtcNow);

        var closingMerit = await db.MeritListEntries.AsNoTracking()
            .Where(item => item.MeritListId == list.Id && item.SeatAllocated)
            .OrderByDescending(item => item.MeritRank)
            .Select(item => (decimal?)item.MeritScore)
            .FirstOrDefaultAsync(ct);
        var waitlistedApplicationIds = await db.MeritListEntries.AsNoTracking()
            .Where(item => item.MeritListId == list.Id && !item.SeatAllocated)
            .OrderBy(item => item.MeritRank)
            .Select(item => item.ApplicationId)
            .ToListAsync(ct);
        var waitlistIndex = waitlistedApplicationIds.IndexOf(application.Id);
        var decision = entry.AllocationStatus ?? (entry.SeatAllocated ? "allocated" : "pending_allocation");
        var limitations = new[]
        {
            "The legacy merit list did not persist a policy version or eligibility-requirement evaluation.",
            "Component weights are reported only where they can be recovered exactly from persisted raw values and contributions.",
            "Distance from closing merit is historical arithmetic and does not promise admission after changing any component."
        };
        var components = new[]
        {
            new AdmissionMeritComponent("Matric", application.MatricPercentage, application.MatricPercentage,
                RecoverWeight(application.MatricPercentage, entry.MatricContribution), entry.MatricContribution),
            new AdmissionMeritComponent("FSc", application.FscPercentage, application.FscPercentage,
                RecoverWeight(application.FscPercentage, entry.FscContribution), entry.FscContribution),
            new AdmissionMeritComponent("EntryTest", application.EntryTestScore, null, null, entry.EntryTestContribution)
        };
        var auditId = Guid.NewGuid();
        var inputHash = Hash(new
        {
            application.Id, application.ProgramId, application.QuotaType, application.MatricPercentage, application.FscPercentage,
            application.EntryTestScore, application.ApplicationStatus, EntryId = entry.Id, entry.MeritScore, entry.MeritRank,
            entry.MatricContribution, entry.FscContribution, entry.EntryTestContribution, entry.SeatAllocated,
            entry.AllocationStatus, MeritListId = list.Id, list.TotalSeats, list.RoundNumber, MeritListStatus = list.Status, list.GeneratedAt, list.PublishedAt
        });
        var existing = await ReadDecisionSnapshotAsync<AdmissionDecisionExplanation>("admission", application.Id, inputHash, ct);
        if (existing is not null) return existing;
        var explanation = new AdmissionDecisionExplanation(
            application.Id,
            list.Id,
            decision,
            [],
            components,
            entry.MeritScore,
            entry.MeritRank,
            list.TotalSeats,
            closingMerit,
            closingMerit is null ? null : entry.MeritScore - closingMerit.Value,
            decision == "waitlist" && waitlistIndex >= 0 ? waitlistIndex + 1 : null,
            application.ApplicationStatus == "rejected" ? "Application verification was rejected; no structured reason was persisted." : null,
            null,
            list.PublishedAt ?? list.GeneratedAt,
            auditId,
            limitations);
        return await PersistDecisionSnapshotAsync("admission", "AdmissionApplication", application.Id, ExplanationSource.Rule,
            null, list.PublishedAt ?? list.GeneratedAt, inputHash, explanation, ct, auditId);
    }

    public async Task<AnalyticsEnvelope<PredictionExplanation>?> GetPredictionExplanationAsync(
        Guid predictionId,
        Guid? studentId,
        Guid? departmentScopeId,
        bool tenantWide,
        CancellationToken ct = default)
    {
        db.GetRequiredTenantId();
        var snapshot = await db.AnalyticsModelPredictionSnapshots.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == predictionId, ct);
        if (snapshot is null) return null;
        if (!tenantWide)
        {
            var ownsPrediction = studentId is not null && snapshot.SubjectType == "Student" && snapshot.SubjectId == studentId;
            var departmentAuthorized = departmentScopeId is not null && snapshot.SubjectType == "Student"
                && await StudentDepartmentQuery(snapshot.SubjectId, db.GetRequiredTenantId()).AnyAsync(id => id == departmentScopeId, ct);
            if (!ownsPrediction && !departmentAuthorized) return null;
        }

        var featureValues = DeserializeDictionary(snapshot.FeatureValuesJson);
        var shapValues = DeserializeDictionary(snapshot.ShapValuesJson);
        var contributions = shapValues
            .OrderByDescending(item => Math.Abs(item.Value))
            .Select((item, index) => new ModelFeatureContribution(item.Key, featureValues.GetValueOrDefault(item.Key), item.Value, index + 1))
            .ToArray();
        var explanation = new PredictionExplanation(
            snapshot.Id,
            snapshot.SubjectId,
            snapshot.SubjectType,
            Enum.Parse<ModelKind>(snapshot.ModelKind),
            snapshot.ModelName,
            snapshot.ModelVersion,
            snapshot.FeatureSchemaVersion,
            snapshot.PredictionTimestamp,
            snapshot.DataCutoffTimestamp,
            snapshot.RawPrediction,
            snapshot.CalibratedProbability,
            snapshot.BaselineExpectedValue,
            featureValues,
            shapValues,
            contributions,
            snapshot.ExplanationAlgorithm,
            snapshot.ExplanationAlgorithmVersion,
            "Factors influencing this model prediction",
            snapshot.RequiresHumanReview,
            "SHAP values describe this model prediction, not causes of the real-world outcome.");
        return new AnalyticsEnvelope<PredictionExplanation>(ExplanationKind.MlPrediction, ExplanationSource.Model,
            new Availability(AvailabilityStatus.Available), explanation, ExplanationVersion, snapshot.CreatedAt, snapshot.Id);
    }

    public async Task<ModelQuality?> GetLatestModelQualityAsync(string modelName, string? modelVersion, CancellationToken ct = default)
    {
        db.GetRequiredTenantId();
        var query = db.AnalyticsModelQualitySnapshots.AsNoTracking().Where(item => item.ModelName == modelName);
        if (!string.IsNullOrWhiteSpace(modelVersion)) query = query.Where(item => item.ModelVersion == modelVersion);
        var snapshot = IsSqlite()
            ? (await query.ToListAsync(ct)).MaxBy(item => item.EvaluationTimestamp)
            : await query.OrderByDescending(item => item.EvaluationTimestamp).FirstOrDefaultAsync(ct);
        return snapshot is null ? null : JsonSerializer.Deserialize<ModelQuality>(snapshot.PayloadJson, JsonOptions);
    }

    public async Task<GlobalModelExplanation?> GetLatestGlobalExplanationAsync(string modelName, string? modelVersion, CancellationToken ct = default)
    {
        db.GetRequiredTenantId();
        var query = db.AnalyticsGlobalExplanationSnapshots.AsNoTracking().Where(item => item.ModelName == modelName);
        if (!string.IsNullOrWhiteSpace(modelVersion)) query = query.Where(item => item.ModelVersion == modelVersion);
        var snapshot = IsSqlite()
            ? (await query.ToListAsync(ct)).MaxBy(item => item.CalculatedAt)
            : await query.OrderByDescending(item => item.CalculatedAt).FirstOrDefaultAsync(ct);
        if (snapshot is null) return null;
        return new GlobalModelExplanation(snapshot.Id, snapshot.ModelName, snapshot.ModelVersion, snapshot.FeatureSchemaVersion,
            snapshot.AuthorizedPopulation, snapshot.SampleSize, snapshot.PopulationStart, snapshot.PopulationEnd,
            DeserializeDictionary(snapshot.MeanAbsoluteShapValuesJson), snapshot.ExplanationAlgorithm,
            snapshot.ExplanationAlgorithmVersion, snapshot.CalculatedAt,
            "Global SHAP summarizes this model across the authorized population; it does not establish causes.");
    }

    public async Task<Guid> PersistPredictionSnapshotAsync(PredictionSnapshotWrite snapshot, CancellationToken ct = default)
    {
        var tenantId = db.GetRequiredTenantId();
        ValidatePrediction(snapshot);
        var inputHash = Hash(new
        {
            snapshot.SubjectId, snapshot.SubjectType, snapshot.ModelKind, snapshot.ModelName, snapshot.ModelVersion,
            snapshot.FeatureSchemaVersion, snapshot.PredictionTimestamp, snapshot.DataCutoffTimestamp, snapshot.RawPrediction,
            snapshot.CalibratedProbability, snapshot.BaselineExpectedValue, snapshot.FeatureValues, snapshot.ShapValues,
            snapshot.ExplanationAlgorithm, snapshot.ExplanationAlgorithmVersion, snapshot.RequiresHumanReview
        });
        var existing = await db.AnalyticsModelPredictionSnapshots.AsNoTracking().SingleOrDefaultAsync(item => item.Id == snapshot.PredictionId, ct);
        if (existing is not null)
        {
            if (existing.InputHash != inputHash)
                throw new InvalidOperationException("A historical prediction snapshot cannot be rewritten.");
            return existing.Id;
        }

        db.AnalyticsModelPredictionSnapshots.Add(new AnalyticsModelPredictionSnapshot
        {
            Id = snapshot.PredictionId,
            TenantId = tenantId,
            SubjectId = snapshot.SubjectId,
            SubjectType = snapshot.SubjectType,
            ModelKind = snapshot.ModelKind.ToString(),
            ModelName = snapshot.ModelName,
            ModelVersion = snapshot.ModelVersion,
            FeatureSchemaVersion = snapshot.FeatureSchemaVersion,
            PredictionTimestamp = snapshot.PredictionTimestamp,
            DataCutoffTimestamp = snapshot.DataCutoffTimestamp,
            RawPrediction = snapshot.RawPrediction,
            CalibratedProbability = snapshot.CalibratedProbability,
            BaselineExpectedValue = snapshot.BaselineExpectedValue,
            FeatureValuesJson = JsonSerializer.Serialize(snapshot.FeatureValues, JsonOptions),
            ShapValuesJson = JsonSerializer.Serialize(snapshot.ShapValues, JsonOptions),
            ExplanationAlgorithm = snapshot.ExplanationAlgorithm,
            ExplanationAlgorithmVersion = snapshot.ExplanationAlgorithmVersion,
            InputHash = inputHash,
            RequiresHumanReview = snapshot.RequiresHumanReview,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return snapshot.PredictionId;
    }

    public async Task<Guid> PersistModelQualitySnapshotAsync(ModelQuality quality, string evaluationDataHash, CancellationToken ct = default)
    {
        var tenantId = db.GetRequiredTenantId();
        if (string.IsNullOrWhiteSpace(quality.ModelName) || string.IsNullOrWhiteSpace(quality.ModelVersion)
            || quality.EvaluationTimestamp == default || !IsSha256(evaluationDataHash))
            throw new ArgumentException("Model identity, evaluation timestamp, and a SHA-256 evaluation-data hash are required.", nameof(quality));
        var rates = new[] { quality.CalibrationError, quality.AuRoc, quality.Precision, quality.Recall, quality.Specificity, quality.F1, quality.Threshold };
        if (rates.Any(value => value is < 0 or > 1)
            || new[] { quality.TruePositive, quality.TrueNegative, quality.FalsePositive, quality.FalseNegative }.Any(value => value < 0)
            || quality.TrainingSampleSize < 0 || quality.DriftIndicator < 0)
            throw new ArgumentOutOfRangeException(nameof(quality), "Model quality rates and counts must be within valid bounds.");
        if (quality.TrainingPeriodEnd < quality.TrainingPeriodStart)
            throw new ArgumentException("The model training period is invalid.", nameof(quality));

        evaluationDataHash = evaluationDataHash.ToLowerInvariant();
        var existing = await db.AnalyticsModelQualitySnapshots.AsNoTracking().SingleOrDefaultAsync(item =>
            item.ModelName == quality.ModelName && item.ModelVersion == quality.ModelVersion && item.EvaluationDataHash == evaluationDataHash, ct);
        if (existing is not null) return existing.Id;
        var entity = new AnalyticsModelQualitySnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ModelName = quality.ModelName,
            ModelVersion = quality.ModelVersion,
            PayloadJson = JsonSerializer.Serialize(quality, JsonOptions),
            EvaluationDataHash = evaluationDataHash,
            EvaluationTimestamp = quality.EvaluationTimestamp
        };
        db.AnalyticsModelQualitySnapshots.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<Guid> PersistGlobalExplanationSnapshotAsync(
        GlobalModelExplanation explanation,
        string populationHash,
        ModelKind modelKind,
        CancellationToken ct = default)
    {
        var tenantId = db.GetRequiredTenantId();
        if (modelKind != ModelKind.GenuinePredictiveModel
            || !explanation.ExplanationAlgorithm.Contains("shap", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Global SHAP is only valid for a genuine predictive model.");
        if (explanation.SnapshotId == Guid.Empty || string.IsNullOrWhiteSpace(explanation.ModelName)
            || string.IsNullOrWhiteSpace(explanation.ModelVersion) || string.IsNullOrWhiteSpace(explanation.FeatureSchemaVersion)
            || string.IsNullOrWhiteSpace(explanation.AuthorizedPopulation) || explanation.SampleSize < _minimumCohortSize
            || explanation.MeanAbsoluteShapValues.Count == 0 || explanation.MeanAbsoluteShapValues.Values.Any(value => value < 0)
            || explanation.PopulationEnd < explanation.PopulationStart || !IsSha256(populationHash))
            throw new ArgumentException("Global explanation identity, population, sample, SHAP values, and hash are invalid.", nameof(explanation));
        if (explanation.MeanAbsoluteShapValues.Keys.Any(key => ProhibitedSensitiveFeatures.Contains(key)))
            throw new InvalidOperationException("Protected characteristics cannot be model features.");

        populationHash = populationHash.ToLowerInvariant();
        var existing = await db.AnalyticsGlobalExplanationSnapshots.AsNoTracking().SingleOrDefaultAsync(item =>
            item.ModelName == explanation.ModelName && item.ModelVersion == explanation.ModelVersion && item.PopulationHash == populationHash, ct);
        if (existing is not null) return existing.Id;
        var entity = new AnalyticsGlobalExplanationSnapshot
        {
            Id = explanation.SnapshotId,
            TenantId = tenantId,
            ModelName = explanation.ModelName,
            ModelVersion = explanation.ModelVersion,
            FeatureSchemaVersion = explanation.FeatureSchemaVersion,
            AuthorizedPopulation = explanation.AuthorizedPopulation,
            SampleSize = explanation.SampleSize,
            PopulationStart = explanation.PopulationStart,
            PopulationEnd = explanation.PopulationEnd,
            MeanAbsoluteShapValuesJson = JsonSerializer.Serialize(explanation.MeanAbsoluteShapValues, JsonOptions),
            ExplanationAlgorithm = explanation.ExplanationAlgorithm,
            ExplanationAlgorithmVersion = explanation.ExplanationAlgorithmVersion,
            PopulationHash = populationHash,
            CalculatedAt = explanation.CalculatedAt
        };
        db.AnalyticsGlobalExplanationSnapshots.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<Guid> PersistTelemetrySnapshotAsync(StudentTelemetry telemetry, CancellationToken ct = default)
    {
        var tenantId = db.GetRequiredTenantId();
        if (telemetry.StudentId == Guid.Empty || string.IsNullOrWhiteSpace(telemetry.Period)
            || telemetry.MinimumCohortSize < _minimumCohortSize || telemetry.Metrics.Count == 0)
            throw new ArgumentException("Telemetry identity, period, cohort policy, and metrics are required.", nameof(telemetry));
        if (!await StudentDepartmentQuery(telemetry.StudentId, tenantId).AnyAsync(ct))
            throw new UnauthorizedAccessException("The student is outside the authenticated tenant.");
        foreach (var metric in telemetry.Metrics)
        {
            var comparativeValuePresent = metric.Percentile is not null || metric.CohortMean is not null || metric.CohortMedian is not null
                || metric.StandardDeviation is not null || metric.ZScore is not null || metric.CohortDelta is not null;
            if (metric.SampleSize < _minimumCohortSize && comparativeValuePresent)
                throw new InvalidOperationException("Comparative telemetry must be suppressed below the configured cohort size.");
            if (metric.SampleSize < _minimumCohortSize && metric.Availability.Status != AvailabilityStatus.Suppressed)
                throw new InvalidOperationException("Small-cohort telemetry must explicitly report Suppressed availability.");
        }

        var payload = JsonSerializer.Serialize(telemetry, JsonOptions);
        var inputHash = Hash(payload);
        var existing = await db.AnalyticsStudentTelemetrySnapshots.AsNoTracking()
            .SingleOrDefaultAsync(item => item.StudentId == telemetry.StudentId && item.CourseId == telemetry.CourseId
                && item.Period == telemetry.Period && item.InputHash == inputHash, ct);
        if (existing is not null) return existing.Id;
        var entity = new AnalyticsStudentTelemetrySnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = telemetry.StudentId,
            CourseId = telemetry.CourseId,
            Period = telemetry.Period,
            ComparisonPopulation = telemetry.ComparisonPopulation,
            InputHash = inputHash,
            PayloadJson = payload,
            DataCutoffAt = telemetry.DataCutoffTimestamp,
            FreshnessAt = telemetry.FreshnessTimestamp
        };
        db.AnalyticsStudentTelemetrySnapshots.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    private async Task<StudentTelemetry?> ReadLatestTelemetryAsync(Guid studentId, Guid? courseId, CancellationToken ct)
    {
        var query = db.AnalyticsStudentTelemetrySnapshots.AsNoTracking()
            .Where(item => item.StudentId == studentId && item.CourseId == courseId);
        var snapshot = IsSqlite()
            ? (await query.ToListAsync(ct)).MaxBy(item => item.FreshnessAt)
            : await query.OrderByDescending(item => item.FreshnessAt).FirstOrDefaultAsync(ct);
        return snapshot is null ? null : JsonSerializer.Deserialize<StudentTelemetry>(snapshot.PayloadJson, JsonOptions);
    }

    private IQueryable<Guid> StudentDepartmentQuery(Guid studentId, Guid tenantId) =>
        from student in db.Students.AsNoTracking()
        join batch in db.Batches.AsNoTracking() on student.BatchId equals batch.Id
        join department in db.Departments.AsNoTracking() on batch.DepartmentId equals department.Id
        where student.Id == studentId && department.TenantId == tenantId
        select department.Id;

    private async Task<GradeStatisticalContext> BuildGradeContextAsync(CourseGrade grade, Guid tenantId, CancellationToken ct)
    {
        var values = await (
            from peerGrade in db.CourseGrades.AsNoTracking()
            join student in db.Students.AsNoTracking() on peerGrade.StudentId equals student.Id
            join batch in db.Batches.AsNoTracking() on student.BatchId equals batch.Id
            join department in db.Departments.AsNoTracking() on batch.DepartmentId equals department.Id
            where peerGrade.CourseId == grade.CourseId && peerGrade.SemesterId == grade.SemesterId && department.TenantId == tenantId
            select (double?)peerGrade.PercentageObtained).ToListAsync(ct);
        var comparison = statistics.CompareToCohort((double)grade.PercentageObtained, values, _minimumCohortSize);
        if (comparison.Suppressed)
            return new GradeStatisticalContext(comparison.SampleSize, null, null, null, null, null, [],
                new Availability(AvailabilityStatus.Suppressed, comparison.SuppressionReason));
        var summary = statistics.Summarize(values, histogramBins: 10, minimumValidCount: _minimumCohortSize);
        return new GradeStatisticalContext(comparison.SampleSize, ToDecimal(comparison.CohortMean), ToDecimal(comparison.CohortMedian),
            ToDecimal(comparison.StandardDeviation), ToDecimal(comparison.PercentileRank), ToDecimal(comparison.ZScore),
            summary.Distribution, new Availability(AvailabilityStatus.Available));
    }

    private async Task<AnalyticsEnvelope<T>?> ReadDecisionSnapshotAsync<T>(string explanationType, Guid subjectId, string inputHash, CancellationToken ct)
    {
        var snapshot = await db.AnalyticsDecisionSnapshots.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ExplanationType == explanationType && item.SubjectId == subjectId && item.InputHash == inputHash, ct);
        if (snapshot is null) return null;
        var payload = JsonSerializer.Deserialize<T>(snapshot.PayloadJson, JsonOptions);
        return new AnalyticsEnvelope<T>(ExplanationKind.DeterministicDecision, Enum.Parse<ExplanationSource>(snapshot.DecisionSource),
            new Availability(AvailabilityStatus.Available), payload, snapshot.ExplanationVersion, snapshot.CalculatedAt, snapshot.AuditId);
    }

    private async Task<AnalyticsEnvelope<T>?> ReadLatestDecisionSnapshotAsync<T>(string explanationType, Guid subjectId, CancellationToken ct)
    {
        var query = db.AnalyticsDecisionSnapshots.AsNoTracking()
            .Where(item => item.ExplanationType == explanationType && item.SubjectId == subjectId);
        var snapshot = IsSqlite()
            ? (await query.ToListAsync(ct)).MaxBy(item => item.CalculatedAt)
            : await query.OrderByDescending(item => item.CalculatedAt).FirstOrDefaultAsync(ct);
        if (snapshot is null) return null;
        var payload = JsonSerializer.Deserialize<T>(snapshot.PayloadJson, JsonOptions);
        return new AnalyticsEnvelope<T>(ExplanationKind.DeterministicDecision, Enum.Parse<ExplanationSource>(snapshot.DecisionSource),
            new Availability(AvailabilityStatus.Available), payload, snapshot.ExplanationVersion, snapshot.CalculatedAt, snapshot.AuditId);
    }

    private async Task<AnalyticsEnvelope<T>> PersistDecisionSnapshotAsync<T>(
        string explanationType,
        string subjectType,
        Guid subjectId,
        ExplanationSource source,
        string? policyVersion,
        DateTimeOffset dataCutoff,
        string inputHash,
        T payload,
        CancellationToken ct,
        Guid? auditId = null)
    {
        var entity = new AnalyticsDecisionSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = db.GetRequiredTenantId(),
            ExplanationType = explanationType,
            SubjectType = subjectType,
            SubjectId = subjectId,
            DecisionSource = source.ToString(),
            PolicyVersion = policyVersion,
            ExplanationVersion = ExplanationVersion,
            InputHash = inputHash,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            DataCutoffAt = dataCutoff,
            CalculatedAt = DateTimeOffset.UtcNow,
            AuditId = auditId ?? Guid.NewGuid()
        };
        db.AnalyticsDecisionSnapshots.Add(entity);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.Entry(entity).State = EntityState.Detached;
            var concurrent = await ReadDecisionSnapshotAsync<T>(explanationType, subjectId, inputHash, ct);
            if (concurrent is not null) return concurrent;
            throw;
        }
        return new AnalyticsEnvelope<T>(ExplanationKind.DeterministicDecision, source,
            new Availability(AvailabilityStatus.Available), payload, ExplanationVersion, entity.CalculatedAt, entity.AuditId);
    }

    private static GradeBoundaryExplanation ToBoundary(GradeBand band) =>
        new(band.LetterGrade, band.PercentageFrom, band.PercentageTo, band.GpaPoints);

    private static decimal? RecoverWeight(decimal? rawValue, decimal contribution) =>
        rawValue is > 0 ? Math.Round(contribution / rawValue.Value, 6) : null;

    private static decimal? ToDecimal(double? value) => value is null || !double.IsFinite(value.Value)
        ? null
        : Math.Round((decimal)value.Value, 4);

    private static IReadOnlyDictionary<string, decimal> DeserializeDictionary(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, decimal>>(json, JsonOptions) ?? new Dictionary<string, decimal>();

    private static string Hash<T>(T value)
    {
        var json = value is string text ? text : JsonSerializer.Serialize(value, JsonOptions);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static void ValidatePrediction(PredictionSnapshotWrite snapshot)
    {
        if (snapshot.PredictionId == Guid.Empty || snapshot.SubjectId == Guid.Empty
            || string.IsNullOrWhiteSpace(snapshot.SubjectType) || string.IsNullOrWhiteSpace(snapshot.ModelName)
            || string.IsNullOrWhiteSpace(snapshot.ModelVersion) || string.IsNullOrWhiteSpace(snapshot.FeatureSchemaVersion)
            || string.IsNullOrWhiteSpace(snapshot.ExplanationAlgorithm) || string.IsNullOrWhiteSpace(snapshot.ExplanationAlgorithmVersion))
            throw new ArgumentException("Prediction identity and version metadata are required.", nameof(snapshot));
        if (snapshot.DataCutoffTimestamp > snapshot.PredictionTimestamp)
            throw new ArgumentException("The data cutoff cannot be after the prediction timestamp.", nameof(snapshot));
        if (snapshot.FeatureValues.Keys.Any(key => ProhibitedSensitiveFeatures.Contains(key)))
            throw new InvalidOperationException("Protected characteristics cannot be model features.");

        var isShap = snapshot.ExplanationAlgorithm.Contains("shap", StringComparison.OrdinalIgnoreCase);
        if (isShap && snapshot.ModelKind != ModelKind.GenuinePredictiveModel)
            throw new InvalidOperationException("SHAP is only valid for a genuine predictive model.");
        if (snapshot.ModelKind != ModelKind.GenuinePredictiveModel && snapshot.ShapValues.Count > 0)
            throw new InvalidOperationException("Rule, heuristic, and LLM estimates cannot store SHAP values.");
        if (isShap)
        {
            if (snapshot.ShapValues.Count == 0 || snapshot.FeatureValues.Keys.Except(snapshot.ShapValues.Keys).Any()
                || snapshot.ShapValues.Keys.Except(snapshot.FeatureValues.Keys).Any())
                throw new InvalidOperationException("SHAP snapshots require one contribution for every feature value.");
            var reconstructed = snapshot.BaselineExpectedValue + snapshot.ShapValues.Values.Sum();
            if (Math.Abs(reconstructed - snapshot.RawPrediction) > 0.0001m)
                throw new InvalidOperationException("SHAP contributions do not satisfy the prediction additivity check.");
        }
    }

    private static bool IsSha256(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);

    private bool IsSqlite() => string.Equals(db.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal);
}
