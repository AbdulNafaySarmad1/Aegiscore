using AegisCore.Application.CampusOperations;
using AegisCore.Domain.Entities;

namespace AegisCore.Infrastructure.CampusOperations;

public class CampusOperationsService : ICampusOperationsService
{
    private readonly AegisDbContext _db;
    public CampusOperationsService(AegisDbContext db) => _db = db;

    public decimal CalculateLibraryFine(DateTime dueDate, DateTime? returnedDate = null, decimal dailyFine = 10m, decimal maxFine = 500m)
    {
        if (dailyFine < 0 || maxFine < 0) throw new ArgumentOutOfRangeException(nameof(dailyFine), "Fine values cannot be negative.");
        var end = (returnedDate ?? DateTime.UtcNow).Date;
        var daysOverdue = Math.Max(0, (end - dueDate.Date).Days);
        return Math.Min(daysOverdue * dailyFine, maxFine);
    }

    public async Task<IReadOnlyList<LibraryBook>> SearchBooksAsync(string query, CancellationToken ct = default)
    {
        query = (query ?? string.Empty).Trim();
        if (query.Length > 100) throw new ArgumentException("Search query is too long.", nameof(query));
        query = query.ToLowerInvariant();
        var books = _db.LibraryBooks.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query))
        {
            books = books.Where(b => b.Title.ToLower().Contains(query) || (b.Author != null && b.Author.ToLower().Contains(query)) || (b.Isbn != null && b.Isbn.ToLower().Contains(query)));
        }
        return await books.OrderBy(b => b.Title).Take(50).ToListAsync(ct);
    }

    public async Task<TransportCapacityDto> GetTransportCapacityAsync(Guid routeId, CancellationToken ct = default)
    {
        var vehicles = await _db.TransportVehicles.AsNoTracking().Where(v => v.AssignedRouteId == routeId).ToListAsync(ct);
        return new TransportCapacityDto(routeId, vehicles.Count, vehicles.Sum(v => v.Capacity));
    }

    public async Task<VisitorGatePassDto> GenerateVisitorPassAsync(Guid visitorId, DateTime validFrom, DateTime validTo, CancellationToken ct = default)
    {
        if (visitorId == Guid.Empty) throw new ArgumentException("A valid visitor id is required.", nameof(visitorId));
        if (validTo <= validFrom) throw new ArgumentException("Pass expiry must be after start time.", nameof(validTo));
        var visitor = await _db.Visitors.FirstOrDefaultAsync(v => v.Id == visitorId, ct)
            ?? throw new InvalidOperationException("Visitor not found.");
        var passNumber = $"VIS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        visitor.BadgeNumber = passNumber;
        visitor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        var payload = $"NOCTURNE|VISITOR|{visitorId:N}|{passNumber}|{validFrom:O}|{validTo:O}";
        return new VisitorGatePassDto(visitorId, passNumber, payload, validFrom, validTo);
    }
}
