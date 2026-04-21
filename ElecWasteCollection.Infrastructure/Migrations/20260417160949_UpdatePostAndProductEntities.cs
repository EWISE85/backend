using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePostAndProductEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Post_CollectionCompany",
                table: "Post");

            migrationBuilder.DropForeignKey(
                name: "FK_Post_Products",
                table: "Post");

            migrationBuilder.DropForeignKey(
                name: "FK_User_CollectionCompany",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_Post_ProductId",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Post");

            migrationBuilder.RenameColumn(
                name: "Small_Collection_Point",
                table: "Vehicles",
                newName: "CollectionUnit");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_Small_Collection_Point",
                table: "Vehicles",
                newName: "IX_Vehicles_CollectionUnit");

            migrationBuilder.RenameColumn(
                name: "CollectionCompanyId",
                table: "User",
                newName: "CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_User_CollectionCompanyId",
                table: "User",
                newName: "IX_User_CompanyId");

            migrationBuilder.AddColumn<Guid>(
                name: "PostId",
                table: "Products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Products_PostId",
                table: "Products",
                column: "PostId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Post_Company",
                table: "Post",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Product_Post",
                table: "Products",
                column: "PostId",
                principalTable: "Post",
                principalColumn: "PostId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Company",
                table: "User",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Post_Company",
                table: "Post");

            migrationBuilder.DropForeignKey(
                name: "FK_Product_Post",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Company",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_Products_PostId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PostId",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "CollectionUnit",
                table: "Vehicles",
                newName: "Small_Collection_Point");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_CollectionUnit",
                table: "Vehicles",
                newName: "IX_Vehicles_Small_Collection_Point");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "User",
                newName: "CollectionCompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_User_CompanyId",
                table: "User",
                newName: "IX_User_CollectionCompanyId");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Post",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Post_ProductId",
                table: "Post",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Post_CollectionCompany",
                table: "Post",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Post_Products",
                table: "Post",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_User_CollectionCompany",
                table: "User",
                column: "CollectionCompanyId",
                principalTable: "Company",
                principalColumn: "CompanyId");
        }
    }
}
