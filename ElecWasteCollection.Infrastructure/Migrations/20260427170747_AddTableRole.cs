using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTableRole : Migration
    {
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// 1. Tạo bảng Role trước để có gốc tham chiếu
			migrationBuilder.CreateTable(
				name: "Role",
				columns: table => new
				{
					RoleId = table.Column<Guid>(type: "uuid", nullable: false),
					Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
					Status = table.Column<string>(type: "text", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Role", x => x.RoleId);
				});

			// 2. Khai báo các ID cố định để map dữ liệu
			var adminWarehouseId = Guid.NewGuid();
			var collectorId = Guid.NewGuid();
			var userId = Guid.NewGuid();
			var adminId = Guid.NewGuid();
			var recyclingCompanyId = Guid.NewGuid();

			// 3. Chèn dữ liệu Enum vào bảng Role (Data Seeding)
			migrationBuilder.InsertData(
				table: "Role",
				columns: new[] { "RoleId", "Name", "Status" },
				values: new object[,]
				{
					{ adminWarehouseId, "AdminWarehouse", "Đang hoạt động" },
					{ collectorId, "Collector", "Đang hoạt động" },
					{ userId, "User", "Đang hoạt động" },
					{ adminId, "Admin", "Đang hoạt động" },
					{ recyclingCompanyId, "RecyclingCompany", "Đang hoạt động" }
				});

			// 4. Thêm cột RoleId vào bảng User (Để nullable: true để xử lý data cũ)
			migrationBuilder.AddColumn<Guid>(
				name: "RoleId",
				table: "User",
				type: "uuid",
				nullable: true);

			// 5. Chuyển đổi dữ liệu từ cột "Role" (string) cũ sang cột "RoleId" (Guid) mới
			// PostgreSQL yêu cầu dấu ngoặc kép cho tên bảng/cột có chữ hoa
			migrationBuilder.Sql($@"
                UPDATE ""User"" SET ""RoleId"" = '{adminWarehouseId}' WHERE ""Role"" = 'AdminWarehouse';
                UPDATE ""User"" SET ""RoleId"" = '{collectorId}' WHERE ""Role"" = 'Collector';
                UPDATE ""User"" SET ""RoleId"" = '{userId}' WHERE ""Role"" = 'User';
                UPDATE ""User"" SET ""RoleId"" = '{adminId}' WHERE ""Role"" = 'Admin';
                UPDATE ""User"" SET ""RoleId"" = '{recyclingCompanyId}' WHERE ""Role"" = 'RecyclingCompany';
                UPDATE ""User"" SET ""RoleId"" = '{userId}' WHERE ""RoleId"" IS NULL;
            ");

			// 6. Sau khi đã map xong, xóa cột string cũ
			migrationBuilder.DropColumn(
				name: "Role",
				table: "User");

			// 7. Chuyển cột RoleId về NOT NULL
			migrationBuilder.AlterColumn<Guid>(
				name: "RoleId",
				table: "User",
				type: "uuid",
				nullable: false,
				oldClrType: typeof(Guid),
				oldType: "uuid",
				oldNullable: true);

			// 8. Tạo Index và Foreign Key
			migrationBuilder.CreateIndex(
				name: "IX_User_RoleId",
				table: "User",
				column: "RoleId");

			migrationBuilder.AddForeignKey(
				name: "FK_User_Role",
				table: "User",
				column: "RoleId",
				principalTable: "Role",
				principalColumn: "RoleId",
				onDelete: ReferentialAction.Cascade);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_User_Role",
				table: "User");

			migrationBuilder.DropIndex(
				name: "IX_User_RoleId",
				table: "User");

			migrationBuilder.AddColumn<string>(
				name: "Role",
				table: "User",
				type: "text",
				nullable: false,
				defaultValue: "");

			// (Tùy chọn) Có thể viết thêm SQL để map ngược ID sang string nếu cần

			migrationBuilder.DropColumn(
				name: "RoleId",
				table: "User");

			migrationBuilder.DropTable(
				name: "Role");
		}
	}
}
