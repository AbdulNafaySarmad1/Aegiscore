using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V1_1_ExaminationSemesterGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SemesterId",
                table: "CourseGrades",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrades_StudentId_SemesterId",
                table: "CourseGrades",
                columns: new[] { "StudentId", "SemesterId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseGrades_StudentId_SemesterId",
                table: "CourseGrades");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "CourseGrades");
        }
    }
}
