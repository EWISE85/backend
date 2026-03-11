using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCapacityToSMP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.AddColumn<double>(
                name: "CurrentCapacity",
                table: "SmallCollectionPoints",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MaxCapacity",
                table: "SmallCollectionPoints",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentCapacity",
                table: "SmallCollectionPoints");

            migrationBuilder.DropColumn(
                name: "MaxCapacity",
                table: "SmallCollectionPoints");

            
        }
    }
}
