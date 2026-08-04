using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Security;

public class GuardManagementService
{
    private readonly ISecurityGuardRepository _guardRepository;
    private readonly IGuardShiftRepository _shiftRepository;
    private readonly IGuardRoundRepository _roundRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GuardManagementService(
        ISecurityGuardRepository guardRepository,
        IGuardShiftRepository shiftRepository,
        IGuardRoundRepository roundRepository,
        IUnitOfWork unitOfWork)
    {
        _guardRepository = guardRepository;
        _shiftRepository = shiftRepository;
        _roundRepository = roundRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SecurityGuard> RegisterGuardAsync(SecurityGuard guard, CancellationToken cancellationToken = default)
    {
        var created = await _guardRepository.AddAsync(guard, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<GuardShift> ScheduleShiftAsync(GuardShift shift, CancellationToken cancellationToken = default)
    {
        shift.Status = ShiftStatus.Scheduled;
        var created = await _shiftRepository.AddAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<GuardRound> StartRoundAsync(Guid guardId, CancellationToken cancellationToken = default)
    {
        var round = new GuardRound
        {
            Id = Guid.NewGuid(),
            SecurityGuardId = guardId,
            RoundStartTime = DateTime.UtcNow
        };

        var created = await _roundRepository.AddAsync(round, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<CheckpointScan> ScanCheckpointAsync(Guid roundId, string checkpointName, decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        var round = await _roundRepository.GetWithScansAsync(roundId, cancellationToken)
            ?? throw new InvalidOperationException("Round not found.");

        var scan = new CheckpointScan
        {
            Id = Guid.NewGuid(),
            GuardRoundId = roundId,
            CheckpointName = checkpointName,
            ScanTime = DateTime.UtcNow,
            Latitude = latitude,
            Longitude = longitude
        };

        round.CheckpointScans.Add(scan);

        await _roundRepository.UpdateAsync(round, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return scan;
    }

    public async Task CompleteRoundAsync(Guid roundId, string notes, CancellationToken cancellationToken = default)
    {
        var round = await _roundRepository.GetByIdAsync(roundId, cancellationToken)
            ?? throw new InvalidOperationException("Round not found.");

        round.RoundEndTime = DateTime.UtcNow;
        round.Notes = notes;

        await _roundRepository.UpdateAsync(round, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
