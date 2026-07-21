namespace AegisCore.Application.DigitalTwin;

public class CampusMapDto
{
    public Guid CampusId { get; set; }
    public string CampusName { get; set; } = string.Empty;
    public int BuildingCount { get; set; }
    public List<BuildingMapDto> Buildings { get; set; } = new();
}

public class BuildingMapDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BuildingCode { get; set; }
    public int FloorCount { get; set; }
    public int RoomCount { get; set; }
    public int OccupiedRooms { get; set; }
    public int ActiveSensors { get; set; }
    public int ActiveTickets { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public List<FloorMapDto> Floors { get; set; } = new();
}

public class FloorMapDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public string? FloorPlanUrl { get; set; }
    public List<RoomMapDto> Rooms { get; set; } = new();
}

public class RoomMapDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public int Capacity { get; set; }
    public string OccupancyState { get; set; } = "Vacant";
    public int? CurrentOccupants { get; set; }
    public string? CurrentEvent { get; set; }
    public DateTime? BookingUntil { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

public class RoomLayoutDto
{
    public Guid RoomId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class SensorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? HardwareId { get; set; }
    public string SensorType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? BuildingId { get; set; }
    public Guid? RoomId { get; set; }
    public decimal? LastValue { get; set; }
    public string? LastUnit { get; set; }
    public DateTime? LastReadingAt { get; set; }
}

public class SensorReadingDto
{
    public Guid Id { get; set; }
    public Guid SensorId { get; set; }
    public decimal Value { get; set; }
    public string? Unit { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class BuildingOverviewDto
{
    public string BuildingName { get; set; } = string.Empty;
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int FreeRooms { get; set; }
    public int SensorCount { get; set; }
    public int OnlineSensors { get; set; }
    public List<SensorDto> Sensors { get; set; } = new();
    public List<RoomMapDto> Rooms { get; set; } = new();
    public EnergySummaryDto? Energy { get; set; }
}

public class EnergySummaryDto
{
    public decimal TotalConsumptionKwh { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? Unit { get; set; }
}

public class IngestSensorDataRequest
{
    public Guid SensorId { get; set; }
    public decimal Value { get; set; }
    public string? Unit { get; set; }
}
