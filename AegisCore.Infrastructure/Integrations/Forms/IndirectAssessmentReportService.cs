using AegisCore.Application.Integrations;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Integrations.Forms;

/// <summary>
/// The indirect (survey) side of outcome-based assessment: what respondents said about each
/// course, its CLOs, its teaching, and — through the existing CLO→PLO mapping — each programme
/// outcome. It reads <see cref="OutcomeSurveyResponse"/> only, so a native response and an
/// external one are counted the same way.
/// </summary>
public sealed class IndirectAssessmentReportService(AegisDbContext db) : IIndirectAssessmentReportService
{
    public async Task<IndirectAssessmentReport> GetReportAsync(Guid? courseId, string? semester, CancellationToken ct = default, Guid? surveyId = null)
    {
        var query = db.OutcomeSurveyResponses.AsNoTracking();
        if (courseId is Guid course) query = query.Where(r => r.CourseId == course);
        if (!string.IsNullOrWhiteSpace(semester)) query = query.Where(r => r.Semester == semester);
        if (surveyId is Guid survey) query = query.Where(r => r.SurveyId == survey);
        var rows = await query.ToListAsync(ct);

        var courseIds = rows.Select(r => r.CourseId).Distinct().ToList();
        var courses = await db.Courses.AsNoTracking().Where(c => courseIds.Contains(c.Id))
            .Select(c => new { c.Id, Code = c.Code ?? "", c.Title }).ToDictionaryAsync(c => c.Id, ct);
        var cloIds = rows.Where(r => r.CourseLearningOutcomeId != null).Select(r => r.CourseLearningOutcomeId!.Value).Distinct().ToList();
        var clos = await db.CourseLearningOutcomes.AsNoTracking().Where(c => cloIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);
        var cloPo = await db.CLOPOMappings.AsNoTracking().Where(m => cloIds.Contains(m.CourseLearningOutcomeId)).ToListAsync(ct);
        var poIds = rows.Where(r => r.ProgramOutcomeId != null).Select(r => r.ProgramOutcomeId!.Value)
            .Concat(cloPo.Select(m => m.ProgramOutcomeId)).Distinct().ToList();
        var pos = await db.ProgramOutcomes.AsNoTracking().Where(p => poIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var courseResults = rows.GroupBy(r => r.CourseId)
            .Select(group =>
            {
                var info = courses.TryGetValue(group.Key, out var c) ? c : null;
                return new CourseIndirectResult(
                    group.Key, info?.Code ?? "", info?.Title ?? "(course removed)",
                    group.Select(r => r.InstructorName).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).Distinct().OrderBy(n => n).ToList(),
                    group.Select(r => r.ResponseKey).Distinct().Count(),
                    Summarise(group),
                    group.Where(r => r.CourseLearningOutcomeId != null).GroupBy(r => r.CourseLearningOutcomeId!.Value)
                        .Select(g => clos.TryGetValue(g.Key, out var clo)
                            ? new OutcomeIndirectResult(g.Key, clo.Code, clo.Description, Summarise(g))
                            : new OutcomeIndirectResult(g.Key, "(removed)", "", Summarise(g)))
                        .OrderBy(o => o.Code, StringComparer.OrdinalIgnoreCase).ToList(),
                    group.Where(r => r.Category != null).GroupBy(r => r.Category!)
                        .Select(g => new CategoryIndirectResult(g.Key, Summarise(g)))
                        .OrderBy(s => s.Category, StringComparer.OrdinalIgnoreCase).ToList());
            })
            .OrderBy(c => c.Code, StringComparer.OrdinalIgnoreCase).ToList();

        var instructors = rows.Where(r => !string.IsNullOrWhiteSpace(r.InstructorName)).GroupBy(r => r.InstructorName!)
            .Select(g => new InstructorIndirectResult(g.Key,
                g.Select(r => courses.TryGetValue(r.CourseId, out var c) ? c.Code : "").Where(code => code.Length > 0).Distinct().OrderBy(code => code).ToList(),
                g.Select(r => r.ResponseKey).Distinct().Count(),
                // Teaching is judged on the teaching items where the form has them, otherwise on everything asked about the course.
                Summarise(g.Any(r => r.Category != null) ? g.Where(r => r.Category != null) : g)))
            .OrderBy(i => i.Instructor, StringComparer.OrdinalIgnoreCase).ToList();

        var programOutcomes = pos.Values.Select(po =>
        {
            // Same weights as the direct attainment (OBEAttainmentService): Low 0.33, Medium 0.66, High 1.
            var weighted = new List<(decimal Percent, decimal Weight)>();
            var contributing = new List<string>();
            foreach (var r in rows.Where(r => r.ProgramOutcomeId == po.Id)) weighted.Add((Percent(r), 1m));
            foreach (var mapping in cloPo.Where(m => m.ProgramOutcomeId == po.Id))
            {
                var weight = mapping.Strength switch { MappingStrength.Low => 0.33m, MappingStrength.Medium => 0.66m, _ => 1m };
                var answers = rows.Where(r => r.CourseLearningOutcomeId == mapping.CourseLearningOutcomeId).ToList();
                if (answers.Count == 0) continue;
                foreach (var r in answers) weighted.Add((Percent(r), weight));
                if (clos.TryGetValue(mapping.CourseLearningOutcomeId, out var clo))
                {
                    var code = courses.TryGetValue(clo.CourseId, out var c) ? $"{c.Code} {clo.Code}" : clo.Code;
                    contributing.Add($"{code} ({mapping.Strength})");
                }
            }
            var totalWeight = weighted.Sum(w => w.Weight);
            var percent = totalWeight == 0 ? 0m : Math.Round(weighted.Sum(w => w.Percent * w.Weight) / totalWeight, 1, MidpointRounding.AwayFromZero);
            return new ProgramOutcomeIndirectResult(po.Id, po.Code, po.Description, weighted.Count, percent, contributing.OrderBy(x => x).ToList());
        })
        .Where(p => p.Answers > 0)
        .OrderBy(p => p.Code, StringComparer.OrdinalIgnoreCase).ToList();

        var channels = rows.GroupBy(r => r.Channel)
            .Select(g => new ChannelCount(g.Key, g.Select(r => r.ResponseKey).Distinct().Count(), g.Count()))
            .OrderBy(c => c.Channel).ToList();

        return new IndirectAssessmentReport(
            DateTime.UtcNow, rows.Select(r => r.ResponseKey).Distinct().Count(), rows.Count,
            rows.Count == 0 ? null : rows.Max(r => r.RecordedAt), channels, courseResults, instructors, programOutcomes);
    }

    private static decimal Percent(OutcomeSurveyResponse r) => r.ScaleMax <= 0 ? 0m : r.Score / r.ScaleMax * 100m;

    /// <summary>
    /// Mean on the scale the answers were given on (mixed scales are compared as a share of each
    /// one's maximum and restated on the most common scale), and the share in the top two points.
    /// </summary>
    public static RatingSummary Summarise(IEnumerable<OutcomeSurveyResponse> answers)
    {
        var list = answers.ToList();
        if (list.Count == 0) return new RatingSummary(0, 0m, 5, 0m);
        var scale = list.GroupBy(r => r.ScaleMax).OrderByDescending(g => g.Count()).First().Key;
        var mean = list.Average(r => r.Score / r.ScaleMax) * scale;
        var favourable = list.Count(r => r.Score >= r.ScaleMax - 1) * 100m / list.Count;
        return new RatingSummary(list.Count, Math.Round(mean, 2, MidpointRounding.AwayFromZero), scale,
            Math.Round(favourable, 1, MidpointRounding.AwayFromZero));
    }
}
