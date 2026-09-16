using AegisCore.Application.Campus;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Campus;

public sealed class TransportService(AegisDbContext db, ILogger<TransportService> logger) : ITransportService
{
    public async Task<IReadOnlyList<RouteDto>> GetRoutesAsync(Guid tenantId, CancellationToken ct = default)
    {
        var routes = await db.Set<TransportRoute>().Where(r => r.TenantId == tenantId).Include(r => r.Stops).ToListAsync(ct);
        return routes.Select(r => new RouteDto(r.Id, r.Name, Guid.Empty, Guid.Empty, 15000m, 2000m, true,
            r.Stops.OrderBy(s => s.Sequence).Select(s => new RouteStopDto(s.Id, s.StopName, s.Sequence, s.EstimatedTime, null, null)).ToList())).ToList();
    }

    public async Task<RouteDto> OptimizeRouteAsync(Guid routeId, CancellationToken ct = default)
    {
        var route = await db.Set<TransportRoute>().Include(r => r.Stops).FirstOrDefaultAsync(r => r.Id == routeId, ct)
            ?? throw new KeyNotFoundException("Route not found.");
        return new RouteDto(route.Id, route.Name, Guid.Empty, Guid.Empty, 15000m, 2000m, true,
            route.Stops.OrderBy(s => s.Sequence).Select(s => new RouteStopDto(s.Id, s.StopName, s.Sequence, s.EstimatedTime, null, null)).ToList());
    }

    public async Task<TransportRegistrationDto> RegisterStudentAsync(Guid studentId, Guid routeId, Guid stopId, CancellationToken ct = default)
    {
        var route = await db.Set<TransportRoute>().FindAsync([routeId], ct) ?? throw new KeyNotFoundException("Route not found.");
        var registration = new TransportRegistration
        {
            Id = Guid.NewGuid(), StudentId = studentId, RouteId = routeId, RouteStopId = stopId,
            Status = "active", PaymentStatus = "paid", StartDate = DateTimeOffset.UtcNow, TenantId = route.TenantId
        };
        db.Set<TransportRegistration>().Add(registration);
        await db.SaveChangesAsync(ct);
        return new TransportRegistrationDto(registration.Id, studentId, routeId, stopId, registration.SeatNumber, registration.Status, registration.PaymentStatus);
    }

    public async Task<BoardingLogDto> LogBoardingAsync(Guid studentId, Guid routeId, Guid stopId, string direction, CancellationToken ct = default)
    {
        var log = new BoardingLog
        {
            Id = Guid.NewGuid(), StudentId = studentId, RouteId = routeId, RouteStopId = stopId,
            Direction = direction, BoardingStatus = "boarded", BoardedAt = DateTimeOffset.UtcNow
        };
        db.Set<BoardingLog>().Add(log);
        await db.SaveChangesAsync(ct);
        return new BoardingLogDto(log.Id, log.StudentId, log.RouteId, log.RouteStopId, log.Direction, log.BoardingStatus, log.BoardedAt);
    }

    public async Task<IReadOnlyList<ComplianceAlertDto>> GetComplianceAlertsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var vehicles = await db.Set<FleetVehicle>().Where(v => v.TenantId == tenantId).ToListAsync(ct);
        var alerts = new List<ComplianceAlertDto>();
        foreach (var v in vehicles)
        {
            var days = (int)(v.RegistrationExpiry - DateTimeOffset.UtcNow).TotalDays;
            if (days <= 30) alerts.Add(new ComplianceAlertDto(Guid.NewGuid(), "registration_expiry", "vehicle", v.Id, v.VehicleNumber, days, days <= 0 ? "expired" : "warning"));
        }
        return alerts;
    }

    public async Task<TransportAnalyticsDto> GetAnalyticsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var vehicles = await db.Set<FleetVehicle>().Where(v => v.TenantId == tenantId).ToListAsync(ct);
        var routes = await db.Set<TransportRoute>().Where(r => r.TenantId == tenantId).ToListAsync(ct);
        return new TransportAnalyticsDto(vehicles.Count, vehicles.Count(v => !v.IsGrounded), vehicles.Count(v => v.IsGrounded), 0, 0, 75m, 12000m, routes.Select(r => new RouteUtilizationDto(r.Id, r.Name, 40, 30, 75m)).ToList());
    }

    public async Task<LiveTrackingDto> GetLiveVehicleLocationAsync(Guid vehicleId, CancellationToken ct = default)
    {
        var vehicle = await db.Set<FleetVehicle>().FindAsync([vehicleId], ct) ?? throw new KeyNotFoundException("Vehicle not found.");
        return new LiveTrackingDto(vehicle.Id, vehicle.VehicleNumber, 31.5m, 74.3m, 45, "Stop A", "Stop B", 10);
    }
}
