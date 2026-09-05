using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeolocationToAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GeofenceId",
                table: "Attendances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManualOverride",
                table: "Attendances",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "Latitude",
                table: "Attendances",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationStatus",
                table: "Attendances",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Unverifiable");

            migrationBuilder.AddColumn<byte[]>(
                name: "Longitude",
                table: "Attendances",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideReason",
                table: "Attendances",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CampusPolygons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Vertices = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampusPolygons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_GeofenceId",
                table: "Attendances",
                column: "GeofenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CampusPolygons_TenantId_IsActive",
                table: "CampusPolygons",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_CampusPolygons_GeofenceId",
                table: "Attendances",
                column: "GeofenceId",
                principalTable: "CampusPolygons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_CampusPolygons_GeofenceId",
                table: "Attendances");

            migrationBuilder.DropTable(
                name: "CampusPolygons");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_GeofenceId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "GeofenceId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "IsManualOverride",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "LocationStatus",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "OverrideReason",
                table: "Attendances");
        }
    }
}
