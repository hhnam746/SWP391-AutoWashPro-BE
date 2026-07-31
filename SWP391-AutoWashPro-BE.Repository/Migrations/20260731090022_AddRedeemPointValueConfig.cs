using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddRedeemPointValueConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "system_config",
                columns: new[] { "id", "config_key", "config_value", "created_at", "description", "updated_at", "updated_by" },
                values: new object[] { new Guid("6e44b699-f9f8-4678-bfaa-c572d081ac57"), "RedeemPointValue", "100", new DateTimeOffset(new DateTime(2026, 5, 28, 20, 13, 39, 590, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)), "Value in VND applied per redeemed loyalty point.", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "system_config",
                keyColumn: "id",
                keyValue: new Guid("6e44b699-f9f8-4678-bfaa-c572d081ac57"));
        }
    }
}
