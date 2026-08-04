namespace AegisCore.Application.Campus;

public interface ITransportService
{
    Task<IReadOnlyList<RouteDto>> GetRoutesAsync(Guid tenantId, CancellationToken ct = default);
    Task<RouteDto> OptimizeRouteAsync(Guid routeId, CancellationToken ct = default);
    Task<TransportRegistrationDto> RegisterStudentAsync(Guid studentId, Guid routeId, Guid stopId, CancellationToken ct = default);
    Task<BoardingLogDto> LogBoardingAsync(Guid studentId, Guid routeId, Guid stopId, string direction, CancellationToken ct = default);
    Task<IReadOnlyList<ComplianceAlertDto>> GetComplianceAlertsAsync(Guid tenantId, CancellationToken ct = default);
    Task<TransportAnalyticsDto> GetAnalyticsAsync(Guid tenantId, CancellationToken ct = default);
    Task<LiveTrackingDto> GetLiveVehicleLocationAsync(Guid vehicleId, CancellationToken ct = default);
}

public sealed record RouteDto(
    Guid Id,
    string RouteName,
    Guid? AssignedVehicleId,
    Guid? AssignedDriverId,
    decimal FeePerSemester,
    decimal MonthlyFeeOption,
    bool IsActive,
    List<RouteStopDto> Stops);

public sealed record RouteStopDto(
    Guid Id,
    string StopName,
    int StopOrder,
    string? PickupTime,
    decimal? Latitude,
    decimal? Longitude);

public sealed record TransportRegistrationDto(
    Guid Id,
    Guid StudentId,
    Guid RouteId,
    Guid RouteStopId,
    int? SeatNumber,
    string Status,
    string PaymentStatus);

public sealed record BoardingLogDto(
    Guid Id,
    Guid StudentId,
    Guid RouteId,
    Guid RouteStopId,
    string Direction,
    string BoardingStatus,
    DateTimeOffset BoardedAt);

public sealed record ComplianceAlertDto(
    Guid Id,
    string AlertType,
    string EntityType,
    Guid EntityId,
    string EntityName,
    int DaysRemaining,
    string Severity);

public sealed record TransportAnalyticsDto(
    int TotalVehicles,
    int OperationalVehicles,
    int GroundedVehicles,
    int TotalDrivers,
    int SuspendedDrivers,
    decimal AverageRouteUtilizationPercent,
    decimal CostPerStudentPerSemester,
    List<RouteUtilizationDto> RouteUtilization);

public sealed record RouteUtilizationDto(
    Guid RouteId,
    string RouteName,
    int Capacity,
    int CurrentRiders,
    decimal UtilizationPercent);

public sealed record LiveTrackingDto(
    Guid VehicleId,
    string VehicleNumber,
    decimal Latitude,
    decimal Longitude,
    int SpeedKmh,
    string? CurrentStop,
    string? NextStop,
    int? EtaMinutesToNextStop);
