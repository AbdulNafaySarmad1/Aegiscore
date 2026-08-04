using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AegisCore.Infrastructure;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260731000500_FinanceVoucherPqcPublicKey")]
public partial class FinanceVoucherPqcPublicKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>("PqcPublicKey", "FinanceVouchers", type: "bytea", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn("PqcPublicKey", "FinanceVouchers");
}
