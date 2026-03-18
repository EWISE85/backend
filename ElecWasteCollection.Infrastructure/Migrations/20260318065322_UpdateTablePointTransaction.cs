using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTablePointTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PointTransactions_Product",
                table: "PointTransactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "PointTransactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "VoucherId",
                table: "PointTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_VoucherId",
                table: "PointTransactions",
                column: "VoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_PointTransactions_Product",
                table: "PointTransactions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_PointTransactions_Voucher",
                table: "PointTransactions",
                column: "VoucherId",
                principalTable: "Voucher",
                principalColumn: "VoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PointTransactions_Product",
                table: "PointTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_PointTransactions_Voucher",
                table: "PointTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PointTransactions_VoucherId",
                table: "PointTransactions");

            migrationBuilder.DropColumn(
                name: "VoucherId",
                table: "PointTransactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "PointTransactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PointTransactions_Product",
                table: "PointTransactions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
