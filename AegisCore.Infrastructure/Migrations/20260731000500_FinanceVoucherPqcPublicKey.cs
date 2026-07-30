using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

public partial class FinanceVoucherPqcPublicKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>("PqcPublicKey", "FinanceVouchers", type: "bytea", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn("PqcPublicKey", "FinanceVouchers");
}
