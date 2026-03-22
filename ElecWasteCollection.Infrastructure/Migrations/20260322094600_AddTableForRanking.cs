using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTableForRanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentRankId",
                table: "User",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalCo2Saved",
                table: "User",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DefaultWeight",
                table: "Category",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EmissionFactor",
                table: "Category",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "Rank",
                columns: table => new
                {
                    RankId = table.Column<Guid>(type: "uuid", nullable: false),
                    RankName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MinCo2 = table.Column<double>(type: "double precision", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rank", x => x.RankId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_CurrentRankId",
                table: "User",
                column: "CurrentRankId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Rank",
                table: "User",
                column: "CurrentRankId",
                principalTable: "Rank",
                principalColumn: "RankId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Rank",
                table: "User");

            migrationBuilder.DropTable(
                name: "Rank");

            migrationBuilder.DropIndex(
                name: "IX_User_CurrentRankId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CurrentRankId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "TotalCo2Saved",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DefaultWeight",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "EmissionFactor",
                table: "Category");
        }
    }
}
