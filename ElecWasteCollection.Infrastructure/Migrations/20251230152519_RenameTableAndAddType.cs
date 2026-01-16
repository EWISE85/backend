using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class RenameTableAndAddType : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// --- 1. ĐỔI TÊN BẢNG ---
			migrationBuilder.RenameTable(
				name: "CollectionCompany",
				newName: "Company");

			// --- 2. ĐỔI TÊN CỘT ID ---
			migrationBuilder.RenameColumn(
				name: "CollectionCompanyId",
				table: "Company",
				newName: "CompanyId");

			// --- [QUAN TRỌNG] BỎ LỆNH RENAME INDEX GÂY LỖI ---
			// migrationBuilder.RenameIndex(...) -> XÓA HOẶC COMMENT DÒNG NÀY ĐI

			// --- 3. THÊM CỘT CompanyType ---
			migrationBuilder.AddColumn<string>(
				name: "CompanyType",
				table: "Company",
				type: "text",
				nullable: false,
				defaultValue: "Unknown");

			// --- 4. THÊM CỘT CreateAt CHO USER ---
			migrationBuilder.AddColumn<DateTime>(
				name: "CreateAt",
				table: "User",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			// --- 5. TẠO CÁC INDEX MỚI (BAO GỒM CẢ CÁI CHO COMPANY) ---

			// Index cho User, Product...
			migrationBuilder.CreateIndex(
				name: "IX_User_CreateAt",
				table: "User",
				column: "CreateAt");

			migrationBuilder.CreateIndex(
				name: "IX_SmallCollectionPoints_Created_At",
				table: "SmallCollectionPoints",
				column: "Created_At");

			migrationBuilder.CreateIndex(
				name: "IX_Products_CreateAt",
				table: "Products",
				column: "CreateAt");

			// [THÊM MỚI] Thay vì rename, ta tạo mới luôn cho bảng Company
			// Postgres có cơ chế "If Not Exists" nhưng EF Core thì cứ Create là được
			migrationBuilder.CreateIndex(
				name: "IX_Company_Created_At",
				table: "Company",
				column: "Created_At");

			migrationBuilder.CreateIndex(
				name: "IX_Company_Name",
				table: "Company",
				column: "Name",
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// 1. Xóa các Index mới
			migrationBuilder.DropIndex(name: "IX_User_CreateAt", table: "User");
			migrationBuilder.DropIndex(name: "IX_SmallCollectionPoints_Created_At", table: "SmallCollectionPoints");
			migrationBuilder.DropIndex(name: "IX_Products_CreateAt", table: "Products");
			migrationBuilder.DropIndex(name: "IX_Company_Created_At", table: "Company");
			migrationBuilder.DropIndex(name: "IX_Company_Name", table: "Company");

			// 2. Xóa cột mới
			migrationBuilder.DropColumn(name: "CreateAt", table: "User");
			migrationBuilder.DropColumn(name: "CompanyType", table: "Company");

			// 3. Đổi tên cột về cũ
			migrationBuilder.RenameColumn(
				name: "CompanyId",
				table: "Company",
				newName: "CollectionCompanyId");

			// 4. Đổi tên bảng về cũ
			migrationBuilder.RenameTable(
				name: "Company",
				newName: "CollectionCompany");

			// Không cần RenameIndex ngược lại vì ở Up ta đã Create mới chứ không Rename.
		}
	}
}