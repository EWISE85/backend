using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElecWasteCollection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnEventIdInTableNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "Notification",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Notification");
        }
    }
}
