using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableSystemConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SystemConfig_Company_CompanyId",
                table: "SystemConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemConfig_SmallCollectionPoints_SmallCollectionPointsId",
                table: "SystemConfig");

            migrationBuilder.DropIndex(
                name: "IX_SystemConfig_SmallCollectionPointsId",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "SmallCollectionPointsId",
                table: "SystemConfig");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfig_SmallCollectionPointId",
                table: "SystemConfig",
                column: "SmallCollectionPointId");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemConfig_Company",
                table: "SystemConfig",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemConfig_SmallCollectionPoints",
                table: "SystemConfig",
                column: "SmallCollectionPointId",
                principalTable: "SmallCollectionPoints",
                principalColumn: "SmallCollectionPointsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SystemConfig_Company",
                table: "SystemConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemConfig_SmallCollectionPoints",
                table: "SystemConfig");

            migrationBuilder.DropIndex(
                name: "IX_SystemConfig_SmallCollectionPointId",
                table: "SystemConfig");

            migrationBuilder.AddColumn<string>(
                name: "SmallCollectionPointsId",
                table: "SystemConfig",
                type: "text",
                nullable: true);

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
    }
}
