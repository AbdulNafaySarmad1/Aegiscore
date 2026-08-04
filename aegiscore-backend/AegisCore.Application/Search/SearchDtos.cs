namespace AegisCore.Application.Search;

public class SearchQuery
{
    public string Query { get; set; } = string.Empty;
    public List<SearchFilter>? Filters { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public List<string>? ExcludeFields { get; set; }
}

public class SearchFilter
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class SearchResult<T> where T : class
{
    public long TotalHits { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<T> Hits { get; set; } = new();
    public Dictionary<string, long> Facets { get; set; } = new();
}

public class UnifiedSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public List<string>? Domains { get; set; }
    public string? Department { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid UserId { get; set; }
    public List<string> UserPermissions { get; set; } = new();
}

public class UnifiedSearchResponse
{
    public string Query { get; set; } = string.Empty;
    public long TotalHits { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<SearchGroupResult> Groups { get; set; } = new();
    public long ElapsedMs { get; set; }
}

public class SearchGroupResult
{
    public string Domain { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public long Count { get; set; }
    public List<SearchHitDto> Hits { get; set; } = new();
}

public class SearchHitDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Dictionary<string, string> Highlights { get; set; } = new();
    public string? Url { get; set; }
    public double Score { get; set; }
}

public class SearchableStudent
{
    public string Id { get; set; } = string.Empty;
    public string UniversityId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string TenantId { get; set; } = string.Empty;
}

public class SearchableCourse
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string TenantId { get; set; } = string.Empty;
}
