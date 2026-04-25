using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
	public partial class ChangeProductImageToImage : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// 1. Xóa khóa ngoại cũ trên bảng cũ trước
			migrationBuilder.DropForeignKey(
				name: "FK_ProductImages_Products",
				table: "ProductImages");

			// 2. THÊM LỆNH ĐỔI TÊN BẢNG (Từ ProductImages -> Images)
			migrationBuilder.RenameTable(
				name: "ProductImages",
				newName: "Images");

			// 3. Từ đây trở xuống, tất cả tham số 'table' phải được đổi thành "Images"
			migrationBuilder.RenameColumn(
				name: "ProductImagesId",
				table: "Images", // Đã sửa
				newName: "Id");

			migrationBuilder.AddColumn<Guid>(
				name: "PostId",
				table: "Images", // Đã sửa
				type: "uuid",
				nullable: true);

			// Đổi tên Index cho đồng bộ với tên bảng mới (từ IX_ProductImages... thành IX_Images...)
			migrationBuilder.CreateIndex(
				name: "IX_Images_PostId",
				table: "Images", // Đã sửa
				column: "PostId");

			migrationBuilder.AddForeignKey(
				name: "FK_Images_Post",
				table: "Images", // Đã sửa
				column: "PostId",
				principalTable: "Post",
				principalColumn: "PostId"); // Lưu ý: Nếu khóa chính của bảng Post là "Id" thì bạn sửa lại chỗ này nhé.

			migrationBuilder.AddForeignKey(
				name: "FK_Images_Products",
				table: "Images", // Đã sửa
				column: "ProductId",
				principalTable: "Products",
				principalColumn: "ProductId",
				onDelete: ReferentialAction.Cascade);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// Đảo ngược lại quá trình của hàm Up()
			migrationBuilder.DropForeignKey(
				name: "FK_Images_Post",
				table: "Images");

			migrationBuilder.DropForeignKey(
				name: "FK_Images_Products",
				table: "Images");

			migrationBuilder.DropIndex(
				name: "IX_Images_PostId",
				table: "Images");

			migrationBuilder.DropColumn(
				name: "PostId",
				table: "Images");

			migrationBuilder.RenameColumn(
				name: "Id",
				table: "Images",
				newName: "ProductImagesId");

			// ĐỔI TÊN BẢNG VỀ LẠI NHƯ CŨ
			migrationBuilder.RenameTable(
				name: "Images",
				newName: "ProductImages");

			// Lắp lại khóa ngoại cũ
			migrationBuilder.AddForeignKey(
				name: "FK_ProductImages_Products",
				table: "ProductImages", // Lúc này bảng đã về tên cũ
				column: "ProductId",
				principalTable: "Products",
				principalColumn: "ProductId",
				onDelete: ReferentialAction.Cascade);
		}
	}
}