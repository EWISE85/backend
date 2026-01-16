using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
