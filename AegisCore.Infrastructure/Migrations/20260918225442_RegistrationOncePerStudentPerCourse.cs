using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RegistrationOncePerStudentPerCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // An installation upgrading to this version may already hold duplicate registrations
            // written before a seat could be claimed atomically; the first of each is kept.
            migrationBuilder.Sql(@"
                DELETE FROM ""RegistrationEnrollments"" a
                USING ""RegistrationEnrollments"" b
                WHERE a.""TenantId"" = b.""TenantId""
                  AND a.""WindowId"" = b.""WindowId""
                  AND a.""StudentId"" = b.""StudentId""
                  AND a.""CourseId"" = b.""CourseId""
                  AND (a.""CreatedAt"" > b.""CreatedAt"" OR (a.""CreatedAt"" = b.""CreatedAt"" AND a.""Id"" > b.""Id""));");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationEnrollments_OncePerStudentPerCourse",
                table: "RegistrationEnrollments",
                columns: new[] { "TenantId", "WindowId", "StudentId", "CourseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistrationEnrollments_OncePerStudentPerCourse",
                table: "RegistrationEnrollments");
        }
    }
}
