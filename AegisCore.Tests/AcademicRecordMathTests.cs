using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.DegreeAudit;

namespace AegisCore.Tests;

public class AcademicRecordMathTests
{
    [Fact]
    public void Cgpa_Weights_Credits_And_Includes_Failures()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var f = Guid.NewGuid();
        var courses = new[] {
            new Course { Id = a, Title = "A", Code = "A", CreditHours = 3 },
            new Course { Id = b, Title = "B", Code = "B", CreditHours = 1 },
            new Course { Id = f, Title = "F", Code = "F", CreditHours = 2 }
        };
        var records = new[] {
            new Enrollment { CourseId = a, Grade = "A", GpaPoints = 4 },
            new Enrollment { CourseId = b, Grade = "B", GpaPoints = 3 },
            new Enrollment { CourseId = f, Grade = "F", GpaPoints = 0 }
        };
        Assert.Equal(2.5m, AcademicRecordMath.Cgpa(records, courses));
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("F")] [InlineData("W")] [InlineData("I")]
    public void Nonpassing_Records_Do_Not_Earn_Credits(string? grade)
        => Assert.False(AcademicRecordMath.IsPassing(grade));
}
