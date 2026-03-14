using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColunmPointsInTableUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Brand_Category",
                table: "Brand");

            migrationBuilder.DropIndex(
                name: "IX_Brand_CategoryId",
                table: "Brand");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Brand");

            migrationBuilder.AddColumn<double>(
                name: "Points",
                table: "User",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Points",
                table: "User");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Brand",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Brand_CategoryId",
                table: "Brand",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Brand_Category",
                table: "Brand",
                column: "CategoryId",
                principalTable: "Category",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
