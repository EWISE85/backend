using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableUserPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserPoints_Products_ProductsProductId",
                table: "UserPoints");

            migrationBuilder.DropIndex(
                name: "IX_UserPoints_ProductsProductId",
                table: "UserPoints");

            migrationBuilder.DropColumn(
                name: "ProductsProductId",
                table: "UserPoints");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductsProductId",
                table: "UserPoints",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPoints_ProductsProductId",
                table: "UserPoints",
                column: "ProductsProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserPoints_Products_ProductsProductId",
                table: "UserPoints",
                column: "ProductsProductId",
                principalTable: "Products",
                principalColumn: "ProductId");
        }
    }
}
