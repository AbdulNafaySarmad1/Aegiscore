using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Search;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Search;

public class SearchService : ISearchService
{
    private readonly ISearchIndex<SearchableStudent> _studentIndex;
    private readonly ISearchIndex<SearchableCourse> _courseIndex;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        ISearchIndex<SearchableStudent> studentIndex,
        ISearchIndex<SearchableCourse> courseIndex,
        IUnitOfWork uow,
        ILogger<SearchService> logger)
    {
        _studentIndex = studentIndex;
        _courseIndex = courseIndex;
        _uow = uow;
        _logger = logger;
    }

    public async Task<UnifiedSearchResponse> SearchAsync(string query, UnifiedSearchRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var response = new UnifiedSearchResponse { Query = query, Page = request.Page, PageSize = request.PageSize };

        var searchQuery = new SearchQuery
        {
            Query = query, Page = request.Page, PageSize = request.PageSize,
            Filters = new List<SearchFilter> { new() { Field = "tenantId", Value = request.UserId.ToString() } }
        };

        var domains = request.Domains ?? new List<string> { "students", "courses" };
        var tasks = new List<Task>();

        if (domains.Contains("students"))
            tasks.Add(SearchStudentsAsync(query, request, response, cancellationToken));

        if (domains.Contains("courses"))
            tasks.Add(SearchCoursesAsync(query, request, response, cancellationToken));

        await Task.WhenAll(tasks);

        response.TotalHits = response.Groups.Sum(g => g.Count);
        response.ElapsedMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

        return response;
    }

    private async Task SearchStudentsAsync(string query, UnifiedSearchRequest request, UnifiedSearchResponse response, CancellationToken cancellationToken)
    {
        var result = await _studentIndex.SearchAsync(new SearchQuery
        {
            Query = query, Page = request.Page, PageSize = request.PageSize
        }, cancellationToken);
        var optedOut = (await _uow.Students.FindAsync(student => student.DirectoryOptOut, cancellationToken))
            .Select(student => student.Id.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var visibleHits = result.Hits.Where(hit => !optedOut.Contains(hit.Id)).ToList();

        var group = new SearchGroupResult
        {
            Domain = "students", Label = "Students", Count = visibleHits.Count,
            Hits = visibleHits.Select(h => new SearchHitDto
            {
                Id = h.Id, Title = $"{h.FirstName} {h.LastName}",
                Subtitle = h.UniversityId, Description = $"{h.Department} — {h.Batch}",
                Domain = "students", EntityType = "Student",
                Url = $"/students/{h.Id}", Score = visibleHits.Count > 0 ? 1 : 0
            }).ToList()
        };

        response.Groups.Add(group);
    }

    private async Task SearchCoursesAsync(string query, UnifiedSearchRequest request, UnifiedSearchResponse response, CancellationToken cancellationToken)
    {
        var result = await _courseIndex.SearchAsync(new SearchQuery
        {
            Query = query, Page = request.Page, PageSize = request.PageSize
        }, cancellationToken);

        var group = new SearchGroupResult
        {
            Domain = "courses", Label = "Courses", Count = result.TotalHits,
            Hits = result.Hits.Select(h => new SearchHitDto
            {
                Id = h.Id, Title = $"{h.Code}: {h.Title}",
                Subtitle = $"{h.CreditHours} credits", Description = h.Department,
                Domain = "courses", EntityType = "Course",
                Url = $"/courses/{h.Id}", Score = result.Hits.Count > 0 ? 1 : 0
            }).ToList()
        };

        response.Groups.Add(group);
    }

    public async Task IndexEntityAsync(string entityType, object entity, CancellationToken cancellationToken = default)
    {
        switch (entityType.ToLowerInvariant())
        {
            case "student":
                if (entity is Student s)
                {
                    if (s.DirectoryOptOut)
                        await _studentIndex.DeleteAsync(s.Id.ToString(), cancellationToken);
                    else
                        await _studentIndex.IndexAsync(MapStudent(s), cancellationToken);
                }
                break;
            case "course":
                if (entity is Course c)
                    await _courseIndex.IndexAsync(MapCourse(c), cancellationToken);
                break;
        }
    }

    public async Task DeleteEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        switch (entityType.ToLowerInvariant())
        {
            case "student": await _studentIndex.DeleteAsync(entityId, cancellationToken); break;
            case "course": await _courseIndex.DeleteAsync(entityId, cancellationToken); break;
        }
    }

    public async Task RebuildAllIndicesAsync(CancellationToken cancellationToken = default)
    {
        var students = await _uow.Students.GetAllAsync(cancellationToken);
        var directoryStudents = students.Where(student => !student.DirectoryOptOut).ToList();
        await _studentIndex.RebuildIndexAsync(directoryStudents.Select(MapStudent), cancellationToken);

        var courses = await _uow.Courses.GetAllAsync(cancellationToken);
        await _courseIndex.RebuildIndexAsync(courses.Select(MapCourse), cancellationToken);

        _logger.LogInformation("All search indices rebuilt: {StudentCount} students, {CourseCount} courses",
            students.Count, courses.Count);
    }

    private static SearchableStudent MapStudent(Student s) => new()
    {
        Id = s.Id.ToString(), UniversityId = s.UniversityId ?? "", FirstName = s.FirstName,
        LastName = s.LastName, Email = "", Department = s.Batch?.Department?.Name ?? "", Batch = s.Batch?.Name ?? "",
        Status = s.EnrollmentStatus.ToString(), CreatedAt = s.CreatedAt, TenantId = s.TenantId.ToString()
    };

    private static SearchableCourse MapCourse(Course c) => new()
    {
        Id = c.Id.ToString(), Code = c.Code ?? "", Title = c.Title,
        Department = c.Department?.Name ?? "", CreditHours = c.CreditHours,
        Description = c.CloMappingJson ?? "", CreatedAt = c.CreatedAt, TenantId = c.TenantId.ToString()
    };
}
