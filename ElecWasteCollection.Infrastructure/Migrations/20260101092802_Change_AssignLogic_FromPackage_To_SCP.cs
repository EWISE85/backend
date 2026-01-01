using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Change_AssignLogic_FromPackage_To_SCP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Packages_Company_CompanyId",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_Packages_CompanyId",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Packages");

            migrationBuilder.AddColumn<string>(
                name: "RecyclingCompanyId",
                table: "SmallCollectionPoints",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmallCollectionPoints_RecyclingCompanyId",
                table: "SmallCollectionPoints",
                column: "RecyclingCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_SmallCollectionPoints_RecyclingCompany",
                table: "SmallCollectionPoints",
                column: "RecyclingCompanyId",
                principalTable: "Company",
                principalColumn: "CompanyId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SmallCollectionPoints_RecyclingCompany",
                table: "SmallCollectionPoints");

            migrationBuilder.DropIndex(
                name: "IX_SmallCollectionPoints_RecyclingCompanyId",
                table: "SmallCollectionPoints");

            migrationBuilder.DropColumn(
                name: "RecyclingCompanyId",
                table: "SmallCollectionPoints");

            migrationBuilder.AddColumn<string>(
                name: "CompanyId",
                table: "Packages",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Packages_CompanyId",
                table: "Packages",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Packages_Company_CompanyId",
                table: "Packages",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "CompanyId");
        }
    }
}
