using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceOncePerStudentPerDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // An installation upgrading to this version may already hold duplicates: two roll calls
            // submitted at the same instant both passed the "is it already marked?" read and both
            // inserted. The first record of each student's day is kept and the rest removed, so the
            // index below can be created and the attendance percentages stop double-counting.
            migrationBuilder.Sql(@"
                DELETE FROM ""Attendances"" a
                USING ""Attendances"" b
                WHERE a.""TenantId"" = b.""TenantId""
                  AND a.""CourseId"" = b.""CourseId""
                  AND a.""EnrollmentId"" = b.""EnrollmentId""
                  AND a.""Date"" = b.""Date""
                  AND (a.""CreatedAt"" > b.""CreatedAt"" OR (a.""CreatedAt"" = b.""CreatedAt"" AND a.""Id"" > b.""Id""));");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_OncePerStudentPerDay",
                table: "Attendances",
                columns: new[] { "TenantId", "CourseId", "EnrollmentId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_OncePerStudentPerDay",
                table: "Attendances");
        }
    }
}
