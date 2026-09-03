using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085314_AddTenantId_HumanGradeReview")]
public partial class AddTenantId_HumanGradeReview : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "HumanGradeReviews",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_HumanGradeReviews_TenantId",
            table: "HumanGradeReviews",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_HumanGradeReviews_TenantId",
            table: "HumanGradeReviews");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "HumanGradeReviews");
    }
}
