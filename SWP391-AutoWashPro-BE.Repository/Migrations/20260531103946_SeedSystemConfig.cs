using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking");

            migrationBuilder.InsertData(
                table: "system_config",
                columns: new[] { "id", "config_key", "config_value", "created_at", "description", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("09f7cba0-c348-4654-90d6-bdd3b21385fa"), "BonusPoint", "10", new DateTimeOffset(new DateTime(2026, 5, 28, 20, 13, 39, 590, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)), "Bonus points earned after checkout completed.", null, null },
                    { new Guid("219a17c5-c218-4c0c-b0e0-6e95fd0c6b11"), "PaymentDeposite", "30", new DateTimeOffset(new DateTime(2026, 5, 28, 20, 13, 39, 590, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)), "Deposit percentage required for booking.", null, null },
                    { new Guid("490a0d6b-e4ca-4315-a387-b92b6f52c9bc"), "SlotDurationMinutes", "15", new DateTimeOffset(new DateTime(2026, 5, 28, 20, 13, 39, 590, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)), "Duration of each booking slot in minutes.", null, null },
                    { new Guid("6e830ac7-1934-4392-b05a-b4f777302170"), "WorkingStartHour", "8", new DateTimeOffset(new DateTime(2026, 5, 28, 20, 13, 39, 590, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)), "Default working start time in Vietnam timezone UTC+7.", null, null },
                    { new Guid("8d456f5d-26ba-45f1-a57f-d88234758685"), "WorkingEndHour", "17", new DateTimeOffset(new DateTime(2026, 5, 28, 20, 13, 39, 590, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)), "Default working end time in Vietnam timezone UTC+7.", null, null },
                    { new Guid("f96ce391-eb3a-4a8e-ad76-18c3f8da6668"), "BasePrice", "100000", new DateTimeOffset(new DateTime(2026, 5, 28, 20, 13, 39, 590, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)), "Default base price for service.", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking",
                columns: new[] { "branch_id", "booking_date", "start_time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking");

            migrationBuilder.DeleteData(
                table: "system_config",
                keyColumn: "id",
                keyValue: new Guid("09f7cba0-c348-4654-90d6-bdd3b21385fa"));

            migrationBuilder.DeleteData(
                table: "system_config",
                keyColumn: "id",
                keyValue: new Guid("219a17c5-c218-4c0c-b0e0-6e95fd0c6b11"));

            migrationBuilder.DeleteData(
                table: "system_config",
                keyColumn: "id",
                keyValue: new Guid("490a0d6b-e4ca-4315-a387-b92b6f52c9bc"));

            migrationBuilder.DeleteData(
                table: "system_config",
                keyColumn: "id",
                keyValue: new Guid("6e830ac7-1934-4392-b05a-b4f777302170"));

            migrationBuilder.DeleteData(
                table: "system_config",
                keyColumn: "id",
                keyValue: new Guid("8d456f5d-26ba-45f1-a57f-d88234758685"));

            migrationBuilder.DeleteData(
                table: "system_config",
                keyColumn: "id",
                keyValue: new Guid("f96ce391-eb3a-4a8e-ad76-18c3f8da6668"));

            migrationBuilder.CreateIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking",
                columns: new[] { "branch_id", "booking_date", "start_time" },
                unique: true);
        }
    }
}
