using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectionOffDays_SmallCollectionPoints_SmallCollectionPoin~",
                table: "CollectionOffDays");

            migrationBuilder.DropForeignKey(
                name: "FK_Packages_SmallCollectionPoints",
                table: "Packages");

            migrationBuilder.DropForeignKey(
                name: "FK_Post_SmallCollectionPoints",
                table: "Post");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_SmallCollectionPoints",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemConfig_SmallCollectionPoints",
                table: "SystemConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_User_SmallCollectionPoints",
                table: "User");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_SmallCollectionPoints",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "SmallCollectionPoints");

            migrationBuilder.RenameColumn(
                name: "SmallCollectionPointId",
                table: "User",
                newName: "CollectionUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_User_SmallCollectionPointId",
                table: "User",
                newName: "IX_User_CollectionUnitId");

            migrationBuilder.RenameColumn(
                name: "SmallCollectionPointId",
                table: "SystemConfig",
                newName: "CollectionUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_SystemConfig_SmallCollectionPointId",
                table: "SystemConfig",
                newName: "IX_SystemConfig_CollectionUnitId");

            migrationBuilder.RenameColumn(
                name: "SmallCollectionPointId",
                table: "Products",
                newName: "CollectionUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_SmallCollectionPointId",
                table: "Products",
                newName: "IX_Products_CollectionUnitId");

            migrationBuilder.RenameColumn(
                name: "AssignedSmallPointId",
                table: "Post",
                newName: "AssignedCollectionUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Post_AssignedSmallPointId",
                table: "Post",
                newName: "IX_Post_AssignedCollectionUnitId");

            migrationBuilder.RenameColumn(
                name: "SmallCollectionPointsId",
                table: "Packages",
                newName: "CollectionUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Packages_SmallCollectionPointsId",
                table: "Packages",
                newName: "IX_Packages_CollectionUnitId");

            migrationBuilder.RenameColumn(
                name: "SmallCollectionPointId",
                table: "CollectionOffDays",
                newName: "CollectionUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_CollectionOffDays_SmallCollectionPointId",
                table: "CollectionOffDays",
                newName: "IX_CollectionOffDays_CollectionUnitId");

            migrationBuilder.CreateTable(
                name: "CollectionUnits",
                columns: table => new
                {
                    CollectionUnitId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<string>(type: "text", nullable: false),
                    RecyclingCompanyId = table.Column<string>(type: "text", nullable: true),
                    OpenTime = table.Column<string>(type: "text", nullable: false),
                    MaxCapacity = table.Column<double>(type: "double precision", nullable: false),
                    CurrentCapacity = table.Column<double>(type: "double precision", nullable: false),
                    PlannedCapacity = table.Column<double>(type: "double precision", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionUnits", x => x.CollectionUnitId);
                    table.ForeignKey(
                        name: "FK_CollectionUnit_CollectionCompany",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionUnit_RecyclingCompany",
                        column: x => x.RecyclingCompanyId,
                        principalTable: "Company",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionUnits_CompanyId",
                table: "CollectionUnits",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionUnits_Created_At",
                table: "CollectionUnits",
                column: "Created_At");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionUnits_Name",
                table: "CollectionUnits",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionUnits_RecyclingCompanyId",
                table: "CollectionUnits",
                column: "RecyclingCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionOffDays_CollectionUnits_CollectionUnitId",
                table: "CollectionOffDays",
                column: "CollectionUnitId",
                principalTable: "CollectionUnits",
                principalColumn: "CollectionUnitId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Packages_CollectionUnits",
                table: "Packages",
                column: "CollectionUnitId",
                principalTable: "CollectionUnits",
                principalColumn: "CollectionUnitId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Post_CollectionUnits",
                table: "Post",
                column: "AssignedCollectionUnitId",
                principalTable: "CollectionUnits",
                principalColumn: "CollectionUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_CollectionUnits",
                table: "Products",
                column: "CollectionUnitId",
                principalTable: "CollectionUnits",
                principalColumn: "CollectionUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemConfig_CollectionUnits",
                table: "SystemConfig",
                column: "CollectionUnitId",
                principalTable: "CollectionUnits",
                principalColumn: "CollectionUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_CollectionUnits",
                table: "User",
                column: "CollectionUnitId",
                principalTable: "CollectionUnits",
                principalColumn: "CollectionUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_CollectionUnits",
                table: "Vehicles",
                column: "Small_Collection_Point",
                principalTable: "CollectionUnits",
                principalColumn: "CollectionUnitId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectionOffDays_CollectionUnits_CollectionUnitId",
                table: "CollectionOffDays");

            migrationBuilder.DropForeignKey(
                name: "FK_Packages_CollectionUnits",
                table: "Packages");

            migrationBuilder.DropForeignKey(
                name: "FK_Post_CollectionUnits",
                table: "Post");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_CollectionUnits",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemConfig_CollectionUnits",
                table: "SystemConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_User_CollectionUnits",
                table: "User");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_CollectionUnits",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "CollectionUnits");

            migrationBuilder.RenameColumn(
                name: "CollectionUnitId",
                table: "User",
                newName: "SmallCollectionPointId");

            migrationBuilder.RenameIndex(
                name: "IX_User_CollectionUnitId",
                table: "User",
                newName: "IX_User_SmallCollectionPointId");

            migrationBuilder.RenameColumn(
                name: "CollectionUnitId",
                table: "SystemConfig",
                newName: "SmallCollectionPointId");

            migrationBuilder.RenameIndex(
                name: "IX_SystemConfig_CollectionUnitId",
                table: "SystemConfig",
                newName: "IX_SystemConfig_SmallCollectionPointId");

            migrationBuilder.RenameColumn(
                name: "CollectionUnitId",
                table: "Products",
                newName: "SmallCollectionPointId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_CollectionUnitId",
                table: "Products",
                newName: "IX_Products_SmallCollectionPointId");

            migrationBuilder.RenameColumn(
                name: "AssignedCollectionUnitId",
                table: "Post",
                newName: "AssignedSmallPointId");

            migrationBuilder.RenameIndex(
                name: "IX_Post_AssignedCollectionUnitId",
                table: "Post",
                newName: "IX_Post_AssignedSmallPointId");

            migrationBuilder.RenameColumn(
                name: "CollectionUnitId",
                table: "Packages",
                newName: "SmallCollectionPointsId");

            migrationBuilder.RenameIndex(
                name: "IX_Packages_CollectionUnitId",
                table: "Packages",
                newName: "IX_Packages_SmallCollectionPointsId");

            migrationBuilder.RenameColumn(
                name: "CollectionUnitId",
                table: "CollectionOffDays",
                newName: "SmallCollectionPointId");

            migrationBuilder.RenameIndex(
                name: "IX_CollectionOffDays_CollectionUnitId",
                table: "CollectionOffDays",
                newName: "IX_CollectionOffDays_SmallCollectionPointId");

            migrationBuilder.CreateTable(
                name: "SmallCollectionPoints",
                columns: table => new
                {
                    SmallCollectionPointsId = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<string>(type: "text", nullable: false),
                    RecyclingCompanyId = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentCapacity = table.Column<double>(type: "double precision", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    MaxCapacity = table.Column<double>(type: "double precision", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OpenTime = table.Column<string>(type: "text", nullable: false),
                    PlannedCapacity = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Updated_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmallCollectionPoints", x => x.SmallCollectionPointsId);
                    table.ForeignKey(
                        name: "FK_SmallCollectionPoints_CollectionCompany",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SmallCollectionPoints_RecyclingCompany",
                        column: x => x.RecyclingCompanyId,
                        principalTable: "Company",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmallCollectionPoints_CompanyId",
                table: "SmallCollectionPoints",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SmallCollectionPoints_Created_At",
                table: "SmallCollectionPoints",
                column: "Created_At");

            migrationBuilder.CreateIndex(
                name: "IX_SmallCollectionPoints_Name",
                table: "SmallCollectionPoints",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmallCollectionPoints_RecyclingCompanyId",
                table: "SmallCollectionPoints",
                column: "RecyclingCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionOffDays_SmallCollectionPoints_SmallCollectionPoin~",
                table: "CollectionOffDays",
                column: "SmallCollectionPointId",
                principalTable: "SmallCollectionPoints",
                principalColumn: "SmallCollectionPointsId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Packages_SmallCollectionPoints",
                table: "Packages",
                column: "SmallCollectionPointsId",
                principalTable: "SmallCollectionPoints",
                principalColumn: "SmallCollectionPointsId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Post_SmallCollectionPoints",
                table: "Post",
                column: "AssignedSmallPointId",
                principalTable: "SmallCollectionPoints",
                principalColumn: "SmallCollectionPointsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_SmallCollectionPoints",
                table: "Products",
                column: "SmallCollectionPointId",
                principalTable: "SmallCollectionPoints",
                principalColumn: "SmallCollectionPointsId");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemConfig_SmallCollectionPoints",
                table: "SystemConfig",
                column: "SmallCollectionPointId",
                principalTable: "SmallCollectionPoints",
                principalColumn: "SmallCollectionPointsId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_SmallCollectionPoints",
                table: "User",
                column: "SmallCollectionPointId",
                principalTable: "SmallCollectionPoints",
                principalColumn: "SmallCollectionPointsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_SmallCollectionPoints",
                table: "Vehicles",
                column: "Small_Collection_Point",
                principalTable: "SmallCollectionPoints",
                principalColumn: "SmallCollectionPointsId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
