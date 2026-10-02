using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tevscare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProfileTrackingShoppingPolish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "BedtimeLocal",
                table: "SleepLogs",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "WakeTimeLocal",
                table: "SleepLogs",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualUnitPrice",
                table: "ShoppingStates",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ShoppingStates",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityOverride",
                table: "ShoppingStates",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "Profiles",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.AddColumn<int>(
                name: "Digestion",
                table: "CheckIns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Hunger",
                table: "CheckIns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SleepQuality",
                table: "CheckIns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Steps",
                table: "ActivityLogs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BedtimeLocal",
                table: "SleepLogs");

            migrationBuilder.DropColumn(
                name: "WakeTimeLocal",
                table: "SleepLogs");

            migrationBuilder.DropColumn(
                name: "ActualUnitPrice",
                table: "ShoppingStates");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ShoppingStates");

            migrationBuilder.DropColumn(
                name: "QuantityOverride",
                table: "ShoppingStates");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Digestion",
                table: "CheckIns");

            migrationBuilder.DropColumn(
                name: "Hunger",
                table: "CheckIns");

            migrationBuilder.DropColumn(
                name: "SleepQuality",
                table: "CheckIns");

            migrationBuilder.DropColumn(
                name: "Steps",
                table: "ActivityLogs");
        }
    }
}
