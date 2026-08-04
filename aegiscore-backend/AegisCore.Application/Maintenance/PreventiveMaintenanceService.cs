using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Maintenance;

public class PreventiveMaintenanceService
{
    private readonly IPreventiveMaintenanceScheduleRepository _pmRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PreventiveMaintenanceService(
        IPreventiveMaintenanceScheduleRepository pmRepository,
        IWorkOrderRepository workOrderRepository,
        IUnitOfWork unitOfWork)
    {
        _pmRepository = pmRepository;
        _workOrderRepository = workOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PreventiveMaintenanceSchedule> CreateScheduleAsync(PreventiveMaintenanceSchedule schedule, CancellationToken cancellationToken = default)
    {
        schedule.NextDueAt = CalculateNextDueDate(schedule.Frequency, schedule.IntervalDays, DateTime.UtcNow);
        var created = await _pmRepository.AddAsync(schedule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task GenerateWorkOrdersAsync(CancellationToken cancellationToken = default)
    {
        var dueSchedules = await _pmRepository.GetDueAsync(cancellationToken);

        foreach (var schedule in dueSchedules)
        {
            var workOrder = new WorkOrder
            {
                Id = Guid.NewGuid(),
                WorkOrderNumber = $"PM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                MaintenanceTicketId = Guid.Empty,
                Status = WorkOrderStatus.Assigned,
                IsPreventiveMaintenance = true,
                PMScheduleId = schedule.Id
            };

            await _workOrderRepository.AddAsync(workOrder, cancellationToken);

            schedule.LastPerformedAt = DateTime.UtcNow;
            schedule.NextDueAt = CalculateNextDueDate(schedule.Frequency, schedule.IntervalDays, DateTime.UtcNow);
            schedule.UpdatedAt = DateTime.UtcNow;

            await _pmRepository.UpdateAsync(schedule, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PMDashboardDto> GetPMDashboardAsync(CancellationToken cancellationToken = default)
    {
        var overdueSchedules = await _pmRepository.GetOverdueAsync(cancellationToken);
        var dueSchedules = await _pmRepository.GetDueAsync(cancellationToken);

        return new PMDashboardDto
        {
            OverdueCount = overdueSchedules.Count,
            DueCount = dueSchedules.Count,
            OverdueSchedules = overdueSchedules.Select(s => new PMScheduleDto
            {
                Id = s.Id,
                ScheduleName = s.ScheduleName,
                NextDueAt = s.NextDueAt,
                DaysOverdue = s.NextDueAt.HasValue ? (DateTime.UtcNow - s.NextDueAt.Value).Days : 0
            }).ToList()
        };
    }

    private static DateTime CalculateNextDueDate(PMFrequency frequency, int intervalDays, DateTime fromDate)
    {
        return frequency switch
        {
            PMFrequency.Daily => fromDate.AddDays(intervalDays > 0 ? intervalDays : 1),
            PMFrequency.Weekly => fromDate.AddDays(7 * (intervalDays > 0 ? intervalDays : 1)),
            PMFrequency.Monthly => fromDate.AddMonths(intervalDays > 0 ? intervalDays : 1),
            PMFrequency.Quarterly => fromDate.AddMonths(3 * (intervalDays > 0 ? intervalDays : 1)),
            PMFrequency.SemiAnnually => fromDate.AddMonths(6 * (intervalDays > 0 ? intervalDays : 1)),
            PMFrequency.Annually => fromDate.AddYears(intervalDays > 0 ? intervalDays : 1),
            _ => fromDate.AddMonths(1)
        };
    }
}

public class PMDashboardDto
{
    public int OverdueCount { get; set; }
    public int DueCount { get; set; }
    public List<PMScheduleDto> OverdueSchedules { get; set; } = new();
}

public class PMScheduleDto
{
    public Guid Id { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public DateTime? NextDueAt { get; set; }
    public int DaysOverdue { get; set; }
}
