using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.ResearchLabs;

public class CalibrationService
{
    private readonly ICalibrationRecordRepository _calibrationRepository;
    private readonly ILabEquipmentRepository _equipmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CalibrationService(
        ICalibrationRecordRepository calibrationRepository,
        ILabEquipmentRepository equipmentRepository,
        IUnitOfWork unitOfWork)
    {
        _calibrationRepository = calibrationRepository;
        _equipmentRepository = equipmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CalibrationRecord> RecordCalibrationAsync(CalibrationRecord record, CancellationToken cancellationToken = default)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(record.LabEquipmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Equipment not found.");

        record.Status = CalibrationStatus.Completed;
        var created = await _calibrationRepository.AddAsync(record, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<CalibrationDashboardDto> GetCalibrationDashboardAsync(CancellationToken cancellationToken = default)
    {
        var overdueRecords = await _calibrationRepository.GetOverdueAsync(cancellationToken);

        return new CalibrationDashboardDto
        {
            OverdueCount = overdueRecords.Count,
            OverdueRecords = overdueRecords.Select(r => new CalibrationAlertDto
            {
                Id = r.Id,
                EquipmentName = r.LabEquipment.EquipmentName,
                NextCalibrationDue = r.NextCalibrationDue,
                DaysOverdue = (DateTime.UtcNow - r.NextCalibrationDue).Days
            }).ToList()
        };
    }
}

public class CalibrationDashboardDto
{
    public int OverdueCount { get; set; }
    public List<CalibrationAlertDto> OverdueRecords { get; set; } = new();
}

public class CalibrationAlertDto
{
    public Guid Id { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public DateTime NextCalibrationDue { get; set; }
    public int DaysOverdue { get; set; }
}
