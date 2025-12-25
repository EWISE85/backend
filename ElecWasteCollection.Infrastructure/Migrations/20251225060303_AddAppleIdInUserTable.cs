using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppleIdInUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppleId",
                table: "User",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppleId",
                table: "User");
        }
    }
}
