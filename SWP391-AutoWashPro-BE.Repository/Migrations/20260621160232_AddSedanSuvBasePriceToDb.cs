using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSedanSuvBasePriceToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "system_config",
                columns: new[] { "id", "config_key", "config_value", "description", "created_at" },
                values: new object[,]
                {
                    { Guid.Parse("f1a24c4e-1978-4db9-8d6a-2cb7a3f7f002"), "SedanBasePrice", "0", "Additional base price for Sedan vehicles.", new DateTimeOffset(new DateTime(2026, 5, 28, 20, 13, 39, 590, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)) },
                    { Guid.Parse("8b2e6d2b-0c74-47a0-9c5f-9d83029de001"), "SuvBasePrice", "30000", "Additional base price for SUV vehicles.", new DateTimeOffset(new DateTime(2026, 5, 28, 20, 13, 39, 590, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "system_config",
                keyColumn: "id",
                keyValue: Guid.Parse("f1a24c4e-1978-4db9-8d6a-2cb7a3f7f002"));

            migrationBuilder.DeleteData(
                table: "system_config",
                keyColumn: "id",
                keyValue: Guid.Parse("8b2e6d2b-0c74-47a0-9c5f-9d83029de001"));
        }
    }
}
