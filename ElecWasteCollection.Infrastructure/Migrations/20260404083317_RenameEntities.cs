using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectionUnit_RecyclingCompany",
                table: "CollectionUnits");

            migrationBuilder.DropIndex(
                name: "IX_CollectionUnits_RecyclingCompanyId",
                table: "CollectionUnits");

            migrationBuilder.DropColumn(
                name: "RecyclingCompanyId",
                table: "CollectionUnits");

            migrationBuilder.RenameColumn(
                name: "CollectionCompanyId",
                table: "Post",
                newName: "CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_Post_CollectionCompanyId",
                table: "Post",
                newName: "IX_Post_CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "Post",
                newName: "CollectionCompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_Post_CompanyId",
                table: "Post",
                newName: "IX_Post_CollectionCompanyId");

            migrationBuilder.AddColumn<string>(
                name: "RecyclingCompanyId",
                table: "CollectionUnits",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionUnits_RecyclingCompanyId",
                table: "CollectionUnits",
                column: "RecyclingCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionUnit_RecyclingCompany",
                table: "CollectionUnits",
                column: "RecyclingCompanyId",
                principalTable: "Company",
                principalColumn: "CompanyId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
