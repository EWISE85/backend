using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVehicleDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capacity_M3",
                table: "Vehicles");

            migrationBuilder.AddColumn<double>(
                name: "Height_M",
                table: "Vehicles",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Length_M",
                table: "Vehicles",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Width_M",
                table: "Vehicles",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Height_M",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Length_M",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Width_M",
                table: "Vehicles");

            migrationBuilder.AddColumn<int>(
                name: "Capacity_M3",
                table: "Vehicles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
