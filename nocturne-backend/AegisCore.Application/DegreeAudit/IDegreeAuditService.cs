using DegreeAuditEntity = AegisCore.Domain.Entities.DegreeAudit;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.DegreeAudit;

public interface IDegreeAuditService
{
    Task<IReadOnlyList<DegreeAuditEntity>> RunAuditAsync(Guid studentId, Guid programId, CancellationToken cancellationToken = default);
    Task<StudentProgressDto> GetStudentProgressAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<WhatIfResultDto> RunWhatIfAsync(Guid studentId, List<Guid> proposedCourses, CancellationToken cancellationToken = default);
    Task<DegreeAuditEntity> WaiveRequirementAsync(Guid auditId, string reason, CancellationToken cancellationToken = default);
    Task<CreditTransfer> SubmitCreditTransferAsync(CreditTransfer transfer, CancellationToken cancellationToken = default);
    Task<CreditTransfer> ApproveCreditTransferAsync(Guid transferId, string approvedBy, CancellationToken cancellationToken = default);
}
