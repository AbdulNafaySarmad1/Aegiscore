using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Security;

public class VehiclePassService
{
    private readonly IVehiclePassRepository _vehiclePassRepository;
    private readonly IGateEntryRepository _gateEntryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VehiclePassService(
        IVehiclePassRepository vehiclePassRepository,
        IGateEntryRepository gateEntryRepository,
        IUnitOfWork unitOfWork)
    {
        _vehiclePassRepository = vehiclePassRepository;
        _gateEntryRepository = gateEntryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<VehiclePass> IssueVehiclePassAsync(VehiclePass vehiclePass, CancellationToken cancellationToken = default)
    {
        var existing = await _vehiclePassRepository.GetByVehicleNumberAsync(vehiclePass.VehicleNumber, cancellationToken);
        if (existing != null && existing.Status == VehiclePassStatus.Active)
            throw new InvalidOperationException($"Active pass already exists for vehicle {vehiclePass.VehicleNumber}.");

        vehiclePass.Status = VehiclePassStatus.Active;
        var created = await _vehiclePassRepository.AddAsync(vehiclePass, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task RecordGateEntryAsync(Guid vehiclePassId, string gateName, Guid? guardEmployeeId, CancellationToken cancellationToken = default)
    {
        var pass = await _vehiclePassRepository.GetByIdAsync(vehiclePassId, cancellationToken)
            ?? throw new InvalidOperationException("Vehicle pass not found.");

        if (pass.Status != VehiclePassStatus.Active)
            throw new InvalidOperationException("Vehicle pass is not active.");

        var gateEntry = new GateEntry
        {
            Id = Guid.NewGuid(),
            VehiclePassId = vehiclePassId,
            GateName = gateName,
            EntryTime = DateTime.UtcNow,
            Type = EntryType.Vehicle,
            GuardEmployeeId = guardEmployeeId
        };

        await _gateEntryRepository.AddAsync(gateEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
