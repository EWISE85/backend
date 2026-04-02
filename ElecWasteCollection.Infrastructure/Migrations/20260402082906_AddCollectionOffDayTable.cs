using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionOffDayTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionOffDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<string>(type: "text", nullable: true),
                    SmallCollectionPointId = table.Column<string>(type: "text", nullable: true),
                    OffDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionOffDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionOffDays_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionOffDays_SmallCollectionPoints_SmallCollectionPoin~",
                        column: x => x.SmallCollectionPointId,
                        principalTable: "SmallCollectionPoints",
                        principalColumn: "SmallCollectionPointsId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionOffDays_CompanyId",
                table: "CollectionOffDays",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionOffDays_OffDate",
                table: "CollectionOffDays",
                column: "OffDate");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionOffDays_SmallCollectionPointId",
                table: "CollectionOffDays",
                column: "SmallCollectionPointId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionOffDays");
        }
    }
}
