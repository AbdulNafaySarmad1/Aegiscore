using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.ResearchLabs;

public class SafetyInspectionService
{
    private readonly ILabSafetyInspectionRepository _inspectionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SafetyInspectionService(
        ILabSafetyInspectionRepository inspectionRepository,
        IUnitOfWork unitOfWork)
    {
        _inspectionRepository = inspectionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LabSafetyInspection> ScheduleInspectionAsync(LabSafetyInspection inspection, CancellationToken cancellationToken = default)
    {
        inspection.Status = InspectionStatus.Scheduled;
        var created = await _inspectionRepository.AddAsync(inspection, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task CompleteInspectionAsync(Guid inspectionId, string findings, string correctiveActions, bool isCompliant, CancellationToken cancellationToken = default)
    {
        var inspection = await _inspectionRepository.GetByIdAsync(inspectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Inspection not found.");

        inspection.Status = InspectionStatus.Completed;
        inspection.Findings = findings;
        inspection.CorrectiveActions = correctiveActions;
        inspection.IsCompliant = isCompliant;
        inspection.UpdatedAt = DateTime.UtcNow;

        await _inspectionRepository.UpdateAsync(inspection, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SignOffInspectionAsync(Guid inspectionId, Guid signedOffByUserId, CancellationToken cancellationToken = default)
    {
        var inspection = await _inspectionRepository.GetByIdAsync(inspectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Inspection not found.");

        inspection.Status = InspectionStatus.SignedOff;
        inspection.SignedOffByUserId = signedOffByUserId;
        inspection.SignedOffAt = DateTime.UtcNow;
        inspection.UpdatedAt = DateTime.UtcNow;

        await _inspectionRepository.UpdateAsync(inspection, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
