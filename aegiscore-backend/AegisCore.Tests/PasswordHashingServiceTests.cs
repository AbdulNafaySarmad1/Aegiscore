using AegisCore.Infrastructure.Security;

namespace AegisCore.Tests;

public class PasswordHashingServiceTests
{
    [Fact]
    public void HashPassword_UsesSaltAndVerifiesOriginalPassword()
    {
        var service = new PasswordHashingService();

        var first = service.HashPassword("CorrectHorseBatteryStaple!");
        var second = service.HashPassword("CorrectHorseBatteryStaple!");

        Assert.NotEqual(first, second);
        Assert.True(service.VerifyPassword("CorrectHorseBatteryStaple!", first));
        Assert.False(service.VerifyPassword("wrong-password", first));
    }
}
