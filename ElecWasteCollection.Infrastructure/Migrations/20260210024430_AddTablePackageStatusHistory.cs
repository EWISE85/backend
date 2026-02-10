using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTablePackageStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackageStatusHistory",
                columns: table => new
                {
                    PackageStatusHistoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StatusDescription = table.Column<string>(type: "text", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageStatusHistory", x => x.PackageStatusHistoryId);
                    table.ForeignKey(
                        name: "FK_PackageStatusHistory_Packages",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageStatusHistory_PackageId",
                table: "PackageStatusHistory",
                column: "PackageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageStatusHistory");
        }
    }
}
