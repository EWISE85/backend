using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
	public partial class ChangeProductImageToImageV2 : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// 1. SỬA table thành "Images" (Vì bảng dưới DB hiện tại đã mang tên Images rồi)
			migrationBuilder.DropPrimaryKey(
				name: "PK_ProductImages",
				table: "Images"); // <-- Đã sửa

			// [ĐÃ XÓA] Lệnh RenameTable vì V1 đã làm rồi

			// 2. Đổi tên Index cũ của ProductId cho chuẩn (Giữ nguyên)
			migrationBuilder.RenameIndex(
				name: "IX_ProductImages_ProductId",
				table: "Images",
				newName: "IX_Images_ProductId");

			// [ĐÃ XÓA] Lệnh RenameIndex của PostId vì ở V1 chúng ta đã tạo nó với tên chuẩn luôn rồi

			// 3. Tạo lại khóa chính với tên mới (Giữ nguyên)
			migrationBuilder.AddPrimaryKey(
				name: "PK_Images",
				table: "Images",
				column: "Id");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// Làm ngược lại hàm Up
			migrationBuilder.DropPrimaryKey(
				name: "PK_Images",
				table: "Images");

			migrationBuilder.RenameIndex(
				name: "IX_Images_ProductId",
				table: "Images",
				newName: "IX_ProductImages_ProductId");

			migrationBuilder.AddPrimaryKey(
				name: "PK_ProductImages",
				table: "Images",
				column: "Id");
		}
	}
}