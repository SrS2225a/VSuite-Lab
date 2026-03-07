using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VSuiteLab.Migrations
{
    /// <inheritdoc />
    public partial class VALARMDatesUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Trigger",
                table: "Alarms");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SelectedDate",
                table: "Alarms",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "SelectedTime",
                table: "Alarms",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedDate",
                table: "Alarms");

            migrationBuilder.DropColumn(
                name: "SelectedTime",
                table: "Alarms");

            migrationBuilder.AddColumn<string>(
                name: "Trigger",
                table: "Alarms",
                type: "TEXT",
                nullable: true);
        }
    }
}
