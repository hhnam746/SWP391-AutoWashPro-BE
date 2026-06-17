using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class bookingServiceAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "system_config",
                columns: new[] { "id", "config_key", "config_value", "created_at", "description", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("8b2e6d2b-0c74-47a0-9c5f-9d83029de001"), "SuvBasePrice", "30000", new DateTimeOffset(new DateTime(2026, 5, 28, 20, 13, 39, 590, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)), "Additional base price for SUV vehicles.", null, null },
                    { new Guid("f1a24c4e-1978-4db9-8d6a-2cb7a3f7f002"), "SedanBasePrice", "0", new DateTimeOffset(new DateTime(2026, 5, 28, 20, 13, 39, 590, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)), "Additional base price for Sedan vehicles.", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "system_config",
                keyColumn: "id",
                keyValue: new Guid("8b2e6d2b-0c74-47a0-9c5f-9d83029de001"));

            migrationBuilder.DeleteData(
                table: "system_config",
                keyColumn: "id",
                keyValue: new Guid("f1a24c4e-1978-4db9-8d6a-2cb7a3f7f002"));
        }
    }
}
