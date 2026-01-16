using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableCategoryAttribute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MaxValue",
                table: "CategoryAttributes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinValue",
                table: "CategoryAttributes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "CategoryAttributes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "CategoryAttributes");
        }
    }
}
