using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VSuiteLab.Migrations
{
    /// <inheritdoc />
    public partial class PreviousDavConfigId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UpdateDoNotAsk",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousDavConfigId",
                table: "CalDavItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdateDoNotAsk",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "PreviousDavConfigId",
                table: "CalDavItems");
        }
    }
}
