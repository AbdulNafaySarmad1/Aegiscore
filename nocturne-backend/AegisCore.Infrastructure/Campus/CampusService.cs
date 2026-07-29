using AegisCore.Application.Campus;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Campus;

public class CampusService : ICampusService
{
    private readonly ICampusRepository _campusRepo;
    private readonly IRepository<StudentTransfer> _transferRepo;
    private readonly ICampusConfigurationRepository _configRepo;
    private readonly ILogger<CampusService> _logger;

    public CampusService(
        ICampusRepository campusRepo,
        IRepository<StudentTransfer> transferRepo,
        ICampusConfigurationRepository configRepo,
        ILogger<CampusService> logger)
    {
        _campusRepo = campusRepo;
        _transferRepo = transferRepo;
        _configRepo = configRepo;
        _logger = logger;
    }

    public async Task<CampusDto> RegisterCampusAsync(CreateCampusRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _campusRepo.GetByCodeAsync(request.CampusCode, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Campus code '{request.CampusCode}' already exists.");

        var campus = new Domain.Entities.Campus
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CampusCode = request.CampusCode,
            Address = request.Address,
            City = request.City,
            Country = request.Country,
            Phone = request.Phone,
            Email = request.Email,
            CalendarType = Enum.TryParse<CalendarType>(request.CalendarType, true, out var cal) ? cal : CalendarType.Semester,
            GradingScheme = Enum.TryParse<GradingScheme>(request.GradingScheme, true, out var gs) ? gs : GradingScheme.GPA4,
            IsMain = request.IsMain,
            Status = CampusStatus.Active
        };

        await _campusRepo.AddAsync(campus, cancellationToken);
        _logger.LogInformation("Campus registered: {Name} ({Code})", campus.Name, campus.CampusCode);

        return MapToDto(campus);
    }

    public async Task<IReadOnlyList<CampusDto>> GetCampusesAsync(CancellationToken cancellationToken = default)
    {
        var campuses = await _campusRepo.GetAllAsync(cancellationToken);
        return campuses.Select(MapToDto).ToList();
    }

    public async Task<CampusDto> UpdateCampusAsync(Guid campusId, UpdateCampusRequest request, CancellationToken cancellationToken = default)
    {
        var campus = await _campusRepo.GetByIdAsync(campusId, cancellationToken)
            ?? throw new InvalidOperationException("Campus not found.");

        if (request.Name != null) campus.Name = request.Name;
        if (request.Address != null) campus.Address = request.Address;
        if (request.Phone != null) campus.Phone = request.Phone;
        if (request.Email != null) campus.Email = request.Email;
        if (request.CalendarType != null && Enum.TryParse<CalendarType>(request.CalendarType, true, out var cal)) campus.CalendarType = cal;
        if (request.GradingScheme != null && Enum.TryParse<GradingScheme>(request.GradingScheme, true, out var gs)) campus.GradingScheme = gs;

        campus.UpdatedAt = DateTime.UtcNow;
        await _campusRepo.UpdateAsync(campus, cancellationToken);

        return MapToDto(campus);
    }

    public async Task InitiateStudentTransferAsync(Guid studentId, Guid sourceCampusId, Guid destCampusId, string reason, CancellationToken cancellationToken = default)
    {
        var transfer = new StudentTransfer
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            SourceCampusId = sourceCampusId,
            DestinationCampusId = destCampusId,
            Reason = reason,
            Status = TransferRequestStatus.Submitted
        };

        await _transferRepo.AddAsync(transfer, cancellationToken);
        _logger.LogInformation("Student transfer initiated: Student {StudentId} from {Source} to {Dest}", studentId, sourceCampusId, destCampusId);
    }

    public async Task ApproveTransferAsync(Guid transferId, string approver, bool isSource, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepo.GetByIdAsync(transferId, cancellationToken)
            ?? throw new InvalidOperationException("Transfer not found.");

        if (isSource)
        {
            transfer.Status = TransferRequestStatus.SourceApproved;
            transfer.SourceApprovedBy = approver;
            transfer.SourceApprovedAt = DateTime.UtcNow;
        }
        else
        {
            if (transfer.Status != TransferRequestStatus.SourceApproved)
                throw new InvalidOperationException("Source campus must approve first.");

            transfer.Status = TransferRequestStatus.Completed;
            transfer.DestApprovedBy = approver;
            transfer.DestApprovedAt = DateTime.UtcNow;
            transfer.CompletedAt = DateTime.UtcNow;
        }

        await _transferRepo.UpdateAsync(transfer, cancellationToken);
        _logger.LogInformation("Transfer {TransferId} approved by {Approver} (source: {IsSource})", transferId, approver, isSource);
    }

    public async Task<CampusConfiguration> GetCampusConfigAsync(Guid campusId, CancellationToken cancellationToken = default)
    {
        var config = await _configRepo.GetByCampusAsync(campusId, cancellationToken);
        if (config != null) return config;

        return new CampusConfiguration
        {
            Id = Guid.NewGuid(),
            CampusId = campusId,
            AcademicCalendarJson = "{}",
            GradingPolicyJson = "{}",
            AdmissionRulesJson = "{}",
            FeeStructureJson = "{}",
            PoliciesJson = "{}"
        };
    }

    private static CampusDto MapToDto(Domain.Entities.Campus c)
    {
        return new CampusDto
        {
            Id = c.Id, Name = c.Name, CampusCode = c.CampusCode,
            Address = c.Address, City = c.City, Country = c.Country,
            Phone = c.Phone, Email = c.Email,
            Status = c.Status.ToString(), CalendarType = c.CalendarType.ToString(),
            GradingScheme = c.GradingScheme.ToString(), IsMain = c.IsMain,
            BuildingCount = c.Buildings?.Count ?? 0, CreatedAt = c.CreatedAt
        };
    }
}
