using AegisCore.Infrastructure.CampusOperations;

namespace AegisCore.Tests;

public sealed class CampusOperationsTests
{
    [Fact]
    public void Library_Fine_Is_Capped()
    {
        var service = new CampusOperationsService(null!);
        var fine = service.CalculateLibraryFine(DateTime.UtcNow.AddDays(-100), DateTime.UtcNow, 10m, 500m);
        Assert.Equal(500m, fine);
    }

    [Fact]
    public void Library_Fine_Is_Zero_Before_Due_Date()
    {
        var service = new CampusOperationsService(null!);
        var fine = service.CalculateLibraryFine(DateTime.UtcNow.AddDays(1), DateTime.UtcNow, 10m, 500m);
        Assert.Equal(0m, fine);
    }
}
