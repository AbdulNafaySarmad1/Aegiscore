using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Security;

public class VisitorManagementService
{
    private readonly IVisitorRepository _visitorRepository;
    private readonly IGateEntryRepository _gateEntryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VisitorManagementService(
        IVisitorRepository visitorRepository,
        IGateEntryRepository gateEntryRepository,
        IUnitOfWork unitOfWork)
    {
        _visitorRepository = visitorRepository;
        _gateEntryRepository = gateEntryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Visitor> RegisterVisitorAsync(Visitor visitor, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(visitor.CNIC))
        {
            var existing = await _visitorRepository.GetByCNICAsync(visitor.CNIC, cancellationToken);
            if (existing != null && existing.IsBlacklisted)
                throw new InvalidOperationException("Visitor is blacklisted.");
        }

        visitor.BadgeNumber = $"V-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        visitor.Status = VisitorStatus.Registered;

        var created = await _visitorRepository.AddAsync(visitor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task CheckInVisitorAsync(Guid visitorId, string gateName, Guid? guardEmployeeId, CancellationToken cancellationToken = default)
    {
        var visitor = await _visitorRepository.GetByIdAsync(visitorId, cancellationToken)
            ?? throw new KeyNotFoundException("Visitor not found.");

        if (visitor.IsBlacklisted)
            throw new InvalidOperationException("Visitor is blacklisted.");

        visitor.Status = VisitorStatus.CheckedIn;
        visitor.CheckInTime = DateTime.UtcNow;
        visitor.UpdatedAt = DateTime.UtcNow;

        var gateEntry = new GateEntry
        {
            Id = Guid.NewGuid(),
            VisitorId = visitorId,
            GateName = gateName,
            EntryTime = DateTime.UtcNow,
            Type = EntryType.Visitor,
            GuardEmployeeId = guardEmployeeId
        };

        await _visitorRepository.UpdateAsync(visitor, cancellationToken);
        await _gateEntryRepository.AddAsync(gateEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CheckOutVisitorAsync(Guid visitorId, string gateName, Guid? guardEmployeeId, CancellationToken cancellationToken = default)
    {
        var visitor = await _visitorRepository.GetByIdAsync(visitorId, cancellationToken)
            ?? throw new KeyNotFoundException("Visitor not found.");

        visitor.Status = VisitorStatus.CheckedOut;
        visitor.CheckOutTime = DateTime.UtcNow;
        visitor.UpdatedAt = DateTime.UtcNow;

        var gateEntry = new GateEntry
        {
            Id = Guid.NewGuid(),
            VisitorId = visitorId,
            GateName = gateName,
            EntryTime = DateTime.UtcNow,
            ExitTime = DateTime.UtcNow,
            Type = EntryType.Visitor,
            GuardEmployeeId = guardEmployeeId
        };

        await _visitorRepository.UpdateAsync(visitor, cancellationToken);
        await _gateEntryRepository.AddAsync(gateEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task BlacklistVisitorAsync(Guid visitorId, string reason, CancellationToken cancellationToken = default)
    {
        var visitor = await _visitorRepository.GetByIdAsync(visitorId, cancellationToken)
            ?? throw new KeyNotFoundException("Visitor not found.");

        visitor.IsBlacklisted = true;
        visitor.BlacklistReason = reason;
        visitor.Status = VisitorStatus.Blacklisted;
        visitor.UpdatedAt = DateTime.UtcNow;

        await _visitorRepository.UpdateAsync(visitor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<VisitorDashboardDto> GetVisitorDashboardAsync(CancellationToken cancellationToken = default)
    {
        var checkedInVisitors = await _visitorRepository.GetCheckedInAsync(cancellationToken);
        var blacklistedVisitors = await _visitorRepository.GetBlacklistedAsync(cancellationToken);

        return new VisitorDashboardDto
        {
            CheckedInCount = checkedInVisitors.Count,
            BlacklistedCount = blacklistedVisitors.Count,
            CheckedInVisitors = checkedInVisitors.Select(v => new VisitorDto
            {
                Id = v.Id,
                VisitorName = v.VisitorName,
                Purpose = v.Purpose,
                CheckInTime = v.CheckInTime
            }).ToList()
        };
    }
}

public class VisitorDashboardDto
{
    public int CheckedInCount { get; set; }
    public int BlacklistedCount { get; set; }
    public List<VisitorDto> CheckedInVisitors { get; set; } = new();
}

public class VisitorDto
{
    public Guid Id { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime? CheckInTime { get; set; }
}
