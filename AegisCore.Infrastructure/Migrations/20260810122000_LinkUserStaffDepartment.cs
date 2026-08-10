using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260810122000_LinkUserStaffDepartment")]
public sealed class LinkUserStaffDepartment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "StaffDepartmentId",
            table: "Users",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_StaffDepartmentId",
            table: "Users",
            column: "StaffDepartmentId");

        migrationBuilder.AddForeignKey(
            name: "FK_Users_Departments_StaffDepartmentId",
            table: "Users",
            column: "StaffDepartmentId",
            principalTable: "Departments",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Users_Departments_StaffDepartmentId",
            table: "Users");
        migrationBuilder.DropIndex(
            name: "IX_Users_StaffDepartmentId",
            table: "Users");
        migrationBuilder.DropColumn(
            name: "StaffDepartmentId",
            table: "Users");
    }
}
