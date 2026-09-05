using AegisCore.Domain.Entities;

namespace AegisCore.Application.Geolocation;

public static class GeofenceGeometry
{
    private const decimal BoundaryTolerance = 0.000000000001m;

    public static bool IsPointInPolygon(decimal lat, decimal lon, IReadOnlyList<CampusVertex> polygon)
    {
        if (polygon.Count < 3) return false;

        var inside = false;
        for (var i = 0; i < polygon.Count; i++)
        {
            var j = i == 0 ? polygon.Count - 1 : i - 1;
            var a = polygon[j];
            var b = polygon[i];

            if (IsOnSegment(lat, lon, a, b)) return true;

            var crossesLatitude = (a.Lat > lat) != (b.Lat > lat);
            if (!crossesLatitude) continue;

            var intersectionLon = (b.Lon - a.Lon) * (lat - a.Lat) / (b.Lat - a.Lat) + a.Lon;
            if (lon < intersectionLon) inside = !inside;
        }

        return inside;
    }

    private static bool IsOnSegment(decimal lat, decimal lon, CampusVertex a, CampusVertex b)
    {
        var cross = (lon - a.Lon) * (b.Lat - a.Lat) - (lat - a.Lat) * (b.Lon - a.Lon);
        if (Math.Abs(cross) > BoundaryTolerance) return false;

        return lon >= Math.Min(a.Lon, b.Lon) - BoundaryTolerance
            && lon <= Math.Max(a.Lon, b.Lon) + BoundaryTolerance
            && lat >= Math.Min(a.Lat, b.Lat) - BoundaryTolerance
            && lat <= Math.Max(a.Lat, b.Lat) + BoundaryTolerance;
    }
}
