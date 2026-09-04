using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904003900_AddTenantId_HumanGradeReview")]
public partial class AddTenantId_HumanGradeReview : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
