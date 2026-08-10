using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260810120000_LinkUserStudentIdentity")]
public sealed class LinkUserStudentIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "StudentId",
            table: "Users",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_StudentId",
            table: "Users",
            column: "StudentId",
            unique: true,
            filter: "\"StudentId\" IS NOT NULL");

        migrationBuilder.AddForeignKey(
            name: "FK_Users_Students_StudentId",
            table: "Users",
            column: "StudentId",
            principalTable: "Students",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Users_Students_StudentId",
            table: "Users");
        migrationBuilder.DropIndex(
            name: "IX_Users_StudentId",
            table: "Users");
        migrationBuilder.DropColumn(
            name: "StudentId",
            table: "Users");
    }
}
