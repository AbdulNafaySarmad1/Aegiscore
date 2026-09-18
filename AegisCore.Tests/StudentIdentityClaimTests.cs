using System.IdentityModel.Tokens.Jwt;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// A student's own id travels in the token. /api/session, the fee screens, the transcript and the
/// student dashboard all read the student_id claim, and nothing ever issued it: every student was
/// told their account was not linked to a student record, which made the student half of the portal
/// unreachable by any student.
/// </summary>
public class StudentIdentityClaimTests
{
    private static JwtTokenService Service()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["AEGISCORE_JWT_SECRET"] = Convert.ToBase64String(new byte[32]),
        }).Build();
        return new JwtTokenService(configuration, Mock.Of<IConnectionMultiplexer>());
    }

    private static User Someone() => new()
    {
        Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Username = "u", Email = "u@example.invalid",
        FirstName = "A", LastName = "B",
    };

    [Fact]
    public void A_Students_Own_Id_And_Department_Are_In_Their_Token()
    {
        var user = Someone();
        var studentId = user.Id.ToString();
        var departmentId = Guid.NewGuid().ToString();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(
            Service().GenerateAccessToken(user, new Dictionary<string, string>
            {
                ["student_id"] = studentId,
                ["department_id"] = departmentId,
            }));

        Assert.Equal(studentId, token.Claims.Single(c => c.Type == "student_id").Value);
        Assert.Equal(departmentId, token.Claims.Single(c => c.Type == "department_id").Value);
    }

    [Fact]
    public void A_Staff_Token_Carries_No_Student_Claim()
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(Service().GenerateAccessToken(Someone()));
        Assert.DoesNotContain(token.Claims, c => c.Type == "student_id");
    }

    [Fact]
    public void An_Empty_Claim_Is_Not_Issued()
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(
            Service().GenerateAccessToken(Someone(), new Dictionary<string, string> { ["student_id"] = "" }));
        Assert.DoesNotContain(token.Claims, c => c.Type == "student_id");
    }
}
