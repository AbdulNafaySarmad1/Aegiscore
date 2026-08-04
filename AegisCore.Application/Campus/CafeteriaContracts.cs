namespace AegisCore.Application.Campus;

public interface ICafeteriaService
{
    Task<DailyMenuDto> GetDailyMenuAsync(Guid cafeteriaUnitId, CancellationToken ct = default);
    Task<PreOrderDto> PlacePreOrderAsync(PreOrderRequest request, CancellationToken ct = default);
    Task<CafeteriaTransactionDto> ProcessPaymentAsync(CafeteriaPaymentRequest request, CancellationToken ct = default);
    Task<DemandForecastDto> GetDemandForecastAsync(Guid cafeteriaUnitId, CancellationToken ct = default);
    Task<VendorScorecardDto> GetVendorPerformanceAsync(Guid vendorId, CancellationToken ct = default);
    Task<CafeteriaAnalyticsDto> GetAnalyticsAsync(Guid tenantId, CancellationToken ct = default);
    Task<InventoryAlertDto> GetInventoryAlertsAsync(Guid cafeteriaUnitId, CancellationToken ct = default);
}

public sealed record DailyMenuDto(
    Guid CafeteriaUnitId,
    string CafeteriaName,
    List<MenuItemDto> Items);

public sealed record MenuItemDto(
    Guid Id,
    string ItemName,
    string Category,
    decimal Price,
    int Calories,
    string? AllergensCsv,
    bool IsAvailable);

public sealed record PreOrderRequest(
    Guid StudentId,
    Guid MenuItemId,
    int Quantity);

public sealed record PreOrderDto(
    Guid Id,
    Guid StudentId,
    string ItemName,
    int Quantity,
    decimal TotalAmount,
    string Status,
    DateTimeOffset ReadyAt);

public sealed record CafeteriaPaymentRequest(
    Guid StudentId,
    List<PaymentItem> Items,
    string PaymentMethod);

public sealed record PaymentItem(
    Guid MenuItemId,
    int Quantity);

public sealed record CafeteriaTransactionDto(
    Guid Id,
    decimal TotalAmount,
    decimal WalletDebit,
    decimal MealPlanDebit,
    string PaymentMethod,
    DateTimeOffset TransactionDate);

public sealed record DemandForecastDto(
    Guid CafeteriaUnitId,
    DateTimeOffset ForecastDate,
    List<ItemDemandDto> ItemDemands);

public sealed record ItemDemandDto(
    Guid MenuItemId,
    string ItemName,
    int PredictedDemand,
    int PrepRecommended);

public sealed record VendorScorecardDto(
    Guid VendorId,
    Guid CafeteriaUnitId,
    decimal HygieneScore,
    decimal MenuAdherenceScore,
    decimal ComplaintsScore,
    string OverallRating,
    DateTimeOffset EvaluatedAt);

public sealed record CafeteriaAnalyticsDto(
    decimal TotalDailyRevenue,
    string MostPopularItem,
    string LeastPopularItem,
    string PeakHour,
    decimal AverageSpendPerStudent,
    decimal WastePercentage,
    decimal SatisfactionScore);

public sealed record InventoryAlertDto(
    Guid InventoryItemId,
    string IngredientName,
    decimal QuantityOnHand,
    decimal ReorderPoint,
    bool NeedsReorder);
