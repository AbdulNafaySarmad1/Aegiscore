namespace AegisCore.Application.Obe;

public interface ICisExtractionService
{
    Task<Guid> UploadAsync(Guid courseId, Guid facultyId, Stream file, string fileName, string contentType, CancellationToken ct = default);
    Task ProcessAsync(Guid cisUploadId, CancellationToken ct = default);
    Task ApproveAsync(Guid cisUploadId, Guid reviewerId, CancellationToken ct = default);
}

public interface IHemisExportService
{
    Task<string> ExportProgramAsync(Guid programId, Guid semesterId, CancellationToken ct = default);
}

public interface IOBEAttainmentService
{
    Task<CLOAttainmentResult> CalculateCLOAttainmentAsync(Guid courseId, Guid cloId, Guid semesterId, CancellationToken ct = default);
    Task<POAttainmentResult> CalculatePOAttainmentAsync(Guid programId, Guid poId, Guid semesterId, CancellationToken ct = default);
    Task<ProgramOBEReport> GenerateProgramReportAsync(Guid programId, Guid semesterId, CancellationToken ct = default);
}

public sealed record CLOAttainmentResult(Guid CLOId, string CLOCode, decimal AttainmentPercentage, int TotalStudents, int StudentsMeeting70Threshold);
public sealed record POAttainmentResult(Guid POId, string POCode, decimal AttainmentPercentage, IReadOnlyList<CLOAttainmentResult> MappedCLOs);
public sealed record ProgramOBEReport(Guid ProgramId, Guid SemesterId, decimal AverageAttainment, IReadOnlyList<POAttainmentResult> Outcomes);
