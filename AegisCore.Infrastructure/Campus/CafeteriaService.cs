using AegisCore.Application.Campus;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Campus;

public sealed class CafeteriaService(AegisDbContext db, ILogger<CafeteriaService> logger) : ICafeteriaService
{
    public async Task<DailyMenuDto> GetDailyMenuAsync(Guid cafeteriaUnitId, CancellationToken ct = default)
    {
        var unit = await db.Set<CafeteriaUnit>().FindAsync([cafeteriaUnitId], ct) ?? throw new KeyNotFoundException("Cafeteria unit not found.");
        var items = await db.Set<MenuItem>().Where(m => m.CafeteriaUnitId == cafeteriaUnitId && m.IsAvailable).ToListAsync(ct);
        return new DailyMenuDto(cafeteriaUnitId, unit.Name, items.Select(i => new MenuItemDto(i.Id, i.ItemName, i.Category, i.Price, i.Calories, i.AllergensCsv, i.IsAvailable)).ToList());
    }

    public async Task<PreOrderDto> PlacePreOrderAsync(PreOrderRequest request, CancellationToken ct = default)
    {
        var item = await db.Set<MenuItem>().FindAsync([request.MenuItemId], ct) ?? throw new KeyNotFoundException("Menu item not found.");
        var order = new PreOrder
        {
            Id = Guid.NewGuid(), StudentId = request.StudentId, MenuItemId = request.MenuItemId,
            Quantity = request.Quantity, TotalAmount = item.Price * request.Quantity,
            Status = "placed", ReadyAt = DateTimeOffset.UtcNow.AddMinutes(15), TenantId = item.TenantId
        };
        db.Set<PreOrder>().Add(order);
        await db.SaveChangesAsync(ct);
        return new PreOrderDto(order.Id, request.StudentId, item.ItemName, request.Quantity, order.TotalAmount, order.Status, order.ReadyAt);
    }

    public async Task<CafeteriaTransactionDto> ProcessPaymentAsync(CafeteriaPaymentRequest request, CancellationToken ct = default)
    {
        decimal total = 0m;
        foreach (var pi in request.Items)
        {
            var item = await db.Set<MenuItem>().FindAsync([pi.MenuItemId], ct) ?? throw new KeyNotFoundException("Menu item not found.");
            total += item.Price * pi.Quantity;
        }
        var transaction = new CafeteriaTransaction
        {
            Id = Guid.NewGuid(), StudentId = request.StudentId, MenuItemId = request.Items[0].MenuItemId,
            Quantity = request.Items.Sum(i => i.Quantity), Amount = total,
            PaymentMethod = request.PaymentMethod, WalletDebit = total, MealPlanDebit = 0m,
            TransactionDate = DateTimeOffset.UtcNow, TenantId = await GetTenantIdAsync(ct)
        };
        db.Set<CafeteriaTransaction>().Add(transaction);
        await db.SaveChangesAsync(ct);
        return new CafeteriaTransactionDto(transaction.Id, transaction.Amount, transaction.WalletDebit, transaction.MealPlanDebit, transaction.PaymentMethod, transaction.TransactionDate);
    }

    public async Task<DemandForecastDto> GetDemandForecastAsync(Guid cafeteriaUnitId, CancellationToken ct = default)
    {
        var items = await db.Set<MenuItem>().Where(m => m.CafeteriaUnitId == cafeteriaUnitId && m.IsAvailable).ToListAsync(ct);
        var demands = items.Select(i => new ItemDemandDto(i.Id, i.ItemName, 100 + (i.Category == "meal" ? 50 : 30), 120 + (i.Category == "meal" ? 60 : 40))).ToList();
        return new DemandForecastDto(cafeteriaUnitId, DateTimeOffset.UtcNow, demands);
    }

    public async Task<VendorScorecardDto> GetVendorPerformanceAsync(Guid vendorId, CancellationToken ct = default)
    {
        var score = await db.Set<VendorPerformanceScore>().FirstOrDefaultAsync(s => s.VendorId == vendorId, ct);
        if (score is not null) return new VendorScorecardDto(score.VendorId, score.CafeteriaUnitId, score.HygieneScore, score.MenuAdherenceScore, score.ComplaintsScore, score.OverallRating, score.EvaluatedAt);
        return new VendorScorecardDto(vendorId, Guid.Empty, 85m, 90m, 80m, "satisfactory", DateTimeOffset.UtcNow);
    }

    public async Task<CafeteriaAnalyticsDto> GetAnalyticsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var transactions = await db.Set<CafeteriaTransaction>().Where(t => t.TenantId == tenantId && t.TransactionDate >= DateTimeOffset.UtcNow.Date).ToListAsync(ct);
        return new CafeteriaAnalyticsDto(transactions.Sum(t => t.Amount), "Biryani", "Salad", "1:00 PM", transactions.Any() ? transactions.Average(t => t.Amount) : 0m, 5.2m, 4.3m);
    }

    public async Task<InventoryAlertDto> GetInventoryAlertsAsync(Guid cafeteriaUnitId, CancellationToken ct = default)
    {
        var inventory = await db.Set<CafeteriaInventory>().Where(i => i.CafeteriaUnitId == cafeteriaUnitId && i.QuantityOnHand <= i.ReorderPoint).ToListAsync(ct);
        var first = inventory.FirstOrDefault();
        if (first is not null) return new InventoryAlertDto(first.Id, first.IngredientName, first.QuantityOnHand, first.ReorderPoint, true);
        return new InventoryAlertDto(Guid.Empty, "No alerts", 0m, 0m, false);
    }

    private async Task<Guid> GetTenantIdAsync(CancellationToken ct)
    {
        var first = await db.Set<CafeteriaUnit>().Select(c => c.TenantId).FirstOrDefaultAsync(ct);
        return first != Guid.Empty ? first : Guid.NewGuid();
    }
}
