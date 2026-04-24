using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableUserReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserReport_Route",
                table: "UserReport");

            migrationBuilder.RenameColumn(
                name: "CollectionRouteId",
                table: "UserReport",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_UserReport_CollectionRouteId",
                table: "UserReport",
                newName: "IX_UserReport_ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserReport_Product",
                table: "UserReport",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserReport_Product",
                table: "UserReport");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "UserReport",
                newName: "CollectionRouteId");

            migrationBuilder.RenameIndex(
                name: "IX_UserReport_ProductId",
                table: "UserReport",
                newName: "IX_UserReport_CollectionRouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserReport_Route",
                table: "UserReport",
                column: "CollectionRouteId",
                principalTable: "CollectionRoutes",
                principalColumn: "CollectionRouteId");
        }
    }
}
