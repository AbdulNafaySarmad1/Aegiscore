using AegisCore.Domain.Entities;

namespace AegisCore.Application.CampusOperations;

public interface ICampusOperationsService
{
    decimal CalculateLibraryFine(DateTime dueDate, DateTime? returnedDate = null, decimal dailyFine = 10m, decimal maxFine = 500m);
    Task<IReadOnlyList<LibraryBook>> SearchBooksAsync(string query, CancellationToken cancellationToken = default);
    Task<TransportCapacityDto> GetTransportCapacityAsync(Guid routeId, CancellationToken cancellationToken = default);
    Task<VisitorGatePassDto> GenerateVisitorPassAsync(Guid visitorId, DateTime validFrom, DateTime validTo, CancellationToken cancellationToken = default);
}

public record TransportCapacityDto(Guid RouteId, int Vehicles, int TotalCapacity);
public record VisitorGatePassDto(Guid VisitorId, string PassNumber, string QrPayload, DateTime ValidFrom, DateTime ValidTo);
