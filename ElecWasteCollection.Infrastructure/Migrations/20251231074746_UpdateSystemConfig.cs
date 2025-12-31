using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSystemConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgTravelTimeMinutes",
                table: "SmallCollectionPoints");

            migrationBuilder.DropColumn(
                name: "MaxRoadDistanceKm",
                table: "SmallCollectionPoints");

            migrationBuilder.DropColumn(
                name: "RadiusKm",
                table: "SmallCollectionPoints");

            migrationBuilder.DropColumn(
                name: "ServiceTimeMinutes",
                table: "SmallCollectionPoints");

            migrationBuilder.DropColumn(
                name: "AssignRatio",
                table: "Company");

            migrationBuilder.AddColumn<string>(
                name: "CompanyId",
                table: "SystemConfig",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmallCollectionPointId",
                table: "SystemConfig",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmallCollectionPointsId",
                table: "SystemConfig",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfig_CompanyId",
                table: "SystemConfig",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfig_SmallCollectionPointsId",
                table: "SystemConfig",
                column: "SmallCollectionPointsId");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemConfig_Company_CompanyId",
                table: "SystemConfig",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemConfig_SmallCollectionPoints_SmallCollectionPointsId",
                table: "SystemConfig",
                column: "SmallCollectionPointsId",
                principalTable: "SmallCollectionPoints",
                principalColumn: "SmallCollectionPointsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SystemConfig_Company_CompanyId",
                table: "SystemConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemConfig_SmallCollectionPoints_SmallCollectionPointsId",
                table: "SystemConfig");

            migrationBuilder.DropIndex(
                name: "IX_SystemConfig_CompanyId",
                table: "SystemConfig");

            migrationBuilder.DropIndex(
                name: "IX_SystemConfig_SmallCollectionPointsId",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "SmallCollectionPointId",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "SmallCollectionPointsId",
                table: "SystemConfig");

            migrationBuilder.AddColumn<double>(
                name: "AvgTravelTimeMinutes",
                table: "SmallCollectionPoints",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MaxRoadDistanceKm",
                table: "SmallCollectionPoints",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RadiusKm",
                table: "SmallCollectionPoints",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ServiceTimeMinutes",
                table: "SmallCollectionPoints",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AssignRatio",
                table: "Company",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
