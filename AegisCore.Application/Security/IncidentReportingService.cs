using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Security;

public class IncidentReportingService
{
    private readonly ISecurityIncidentRepository _incidentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IncidentReportingService(
        ISecurityIncidentRepository incidentRepository,
        IUnitOfWork unitOfWork)
    {
        _incidentRepository = incidentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SecurityIncident> ReportIncidentAsync(SecurityIncident incident, CancellationToken cancellationToken = default)
    {
        incident.IncidentNumber = $"INC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        incident.Status = IncidentStatus.Reported;
        incident.IsImmutable = false;

        var created = await _incidentRepository.AddAsync(incident, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task AssignIncidentAsync(Guid incidentId, Guid assignedToEmployeeId, CancellationToken cancellationToken = default)
    {
        var incident = await _incidentRepository.GetByIdAsync(incidentId, cancellationToken)
            ?? throw new KeyNotFoundException("Incident not found.");

        incident.Status = IncidentStatus.UnderInvestigation;
        incident.AssignedToEmployeeId = assignedToEmployeeId;
        incident.UpdatedAt = DateTime.UtcNow;

        await _incidentRepository.UpdateAsync(incident, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResolveIncidentAsync(Guid incidentId, string resolutionNotes, CancellationToken cancellationToken = default)
    {
        var incident = await _incidentRepository.GetByIdAsync(incidentId, cancellationToken)
            ?? throw new KeyNotFoundException("Incident not found.");

        incident.Status = IncidentStatus.Resolved;
        incident.ResolvedAt = DateTime.UtcNow;
        incident.ResolutionNotes = resolutionNotes;
        incident.IsImmutable = true;
        incident.UpdatedAt = DateTime.UtcNow;

        await _incidentRepository.UpdateAsync(incident, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IncidentWitness> AddWitnessAsync(Guid incidentId, IncidentWitness witness, CancellationToken cancellationToken = default)
    {
        var incident = await _incidentRepository.GetWithDetailsAsync(incidentId, cancellationToken)
            ?? throw new KeyNotFoundException("Incident not found.");

        if (incident.IsImmutable)
            throw new InvalidOperationException("Cannot modify a resolved incident.");

        witness.SecurityIncidentId = incidentId;
        incident.Witnesses.Add(witness);

        await _incidentRepository.UpdateAsync(incident, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return witness;
    }

    public async Task<IncidentEvidence> AddEvidenceAsync(Guid incidentId, IncidentEvidence evidence, CancellationToken cancellationToken = default)
    {
        var incident = await _incidentRepository.GetWithDetailsAsync(incidentId, cancellationToken)
            ?? throw new KeyNotFoundException("Incident not found.");

        if (incident.IsImmutable)
            throw new InvalidOperationException("Cannot modify a resolved incident.");

        evidence.SecurityIncidentId = incidentId;
        incident.Evidences.Add(evidence);

        await _incidentRepository.UpdateAsync(incident, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return evidence;
    }

    public async Task<IncidentDashboardDto> GetIncidentDashboardAsync(CancellationToken cancellationToken = default)
    {
        var reportedIncidents = await _incidentRepository.GetByStatusAsync(IncidentStatus.Reported, cancellationToken);
        var underInvestigation = await _incidentRepository.GetByStatusAsync(IncidentStatus.UnderInvestigation, cancellationToken);
        var resolvedIncidents = await _incidentRepository.GetByStatusAsync(IncidentStatus.Resolved, cancellationToken);

        return new IncidentDashboardDto
        {
            ReportedCount = reportedIncidents.Count,
            UnderInvestigationCount = underInvestigation.Count,
            ResolvedCount = resolvedIncidents.Count,
            TotalIncidents = reportedIncidents.Count + underInvestigation.Count + resolvedIncidents.Count
        };
    }
}

public class IncidentDashboardDto
{
    public int ReportedCount { get; set; }
    public int UnderInvestigationCount { get; set; }
    public int ResolvedCount { get; set; }
    public int TotalIncidents { get; set; }
}
