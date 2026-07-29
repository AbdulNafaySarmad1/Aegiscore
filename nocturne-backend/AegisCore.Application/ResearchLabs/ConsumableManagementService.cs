using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.ResearchLabs;

public class ConsumableManagementService
{
    private readonly ILabConsumableRepository _consumableRepository;
    private readonly IRepository<ConsumableRequisition> _requisitionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConsumableManagementService(
        ILabConsumableRepository consumableRepository,
        IRepository<ConsumableRequisition> requisitionRepository,
        IUnitOfWork unitOfWork)
    {
        _consumableRepository = consumableRepository;
        _requisitionRepository = requisitionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LabConsumable> CreateConsumableAsync(LabConsumable consumable, CancellationToken cancellationToken = default)
    {
        var created = await _consumableRepository.AddAsync(consumable, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<ConsumableRequisition> RequestConsumableAsync(ConsumableRequisition requisition, CancellationToken cancellationToken = default)
    {
        var consumable = await _consumableRepository.GetByIdAsync(requisition.LabConsumableId, cancellationToken)
            ?? throw new InvalidOperationException("Consumable not found.");

        if (consumable.QuantityInStock < requisition.Quantity)
            throw new InvalidOperationException("Insufficient stock available.");

        requisition.Status = RequisitionStatus.Pending;
        var created = await _requisitionRepository.AddAsync(requisition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task IssueConsumableAsync(Guid requisitionId, Guid issuedByUserId, CancellationToken cancellationToken = default)
    {
        var requisition = await _requisitionRepository.GetByIdAsync(requisitionId, cancellationToken)
            ?? throw new InvalidOperationException("Requisition not found.");

        if (requisition.Status != RequisitionStatus.Approved)
            throw new InvalidOperationException("Only approved requisitions can be issued.");

        var consumable = await _consumableRepository.GetByIdAsync(requisition.LabConsumableId, cancellationToken)
            ?? throw new InvalidOperationException("Consumable not found.");

        consumable.QuantityInStock -= requisition.Quantity;
        consumable.UpdatedAt = DateTime.UtcNow;

        requisition.Status = RequisitionStatus.Issued;
        requisition.IssuedByUserId = issuedByUserId;
        requisition.IssuedAt = DateTime.UtcNow;

        await _consumableRepository.UpdateAsync(consumable, cancellationToken);
        await _requisitionRepository.UpdateAsync(requisition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<LowStockAlertDto> GetLowStockAlertsAsync(CancellationToken cancellationToken = default)
    {
        var lowStockItems = await _consumableRepository.GetLowStockAsync(cancellationToken);
        var expiringItems = await _consumableRepository.GetExpiringAsync(DateTime.UtcNow.AddMonths(3), cancellationToken);

        return new LowStockAlertDto
        {
            LowStockCount = lowStockItems.Count,
            ExpiringCount = expiringItems.Count,
            LowStockItems = lowStockItems.Select(c => new ConsumableAlertDto
            {
                Id = c.Id,
                ItemName = c.ItemName,
                QuantityInStock = c.QuantityInStock,
                MinimumStockLevel = c.MinimumStockLevel,
                LabName = c.ResearchLab.LabName
            }).ToList(),
            ExpiringItems = expiringItems.Select(c => new ConsumableAlertDto
            {
                Id = c.Id,
                ItemName = c.ItemName,
                QuantityInStock = c.QuantityInStock,
                MinimumStockLevel = c.MinimumStockLevel,
                LabName = c.ResearchLab.LabName
            }).ToList()
        };
    }
}

public class LowStockAlertDto
{
    public int LowStockCount { get; set; }
    public int ExpiringCount { get; set; }
    public List<ConsumableAlertDto> LowStockItems { get; set; } = new();
    public List<ConsumableAlertDto> ExpiringItems { get; set; } = new();
}

public class ConsumableAlertDto
{
    public Guid Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal QuantityInStock { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public string LabName { get; set; } = string.Empty;
}
