using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectorCodeInUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollectorCode",
                table: "User",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectorCode",
                table: "User");
        }
    }
}
