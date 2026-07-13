using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSlotBreakMinutesConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "system_config",
                columns: new[] { "id", "config_key", "config_value", "created_at", "description", "updated_at", "updated_by" },
                values: new object[] { new Guid("7f3b0ad6-9b0b-4c3d-b8d1-5dc1d17a6c4e"), "SlotBreakMinutes", "0", new DateTimeOffset(new DateTime(2026, 5, 28, 20, 13, 39, 590, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)), "Break time in minutes between consecutive booking slots.", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "system_config",
                keyColumn: "id",
                keyValue: new Guid("7f3b0ad6-9b0b-4c3d-b8d1-5dc1d17a6c4e"));
        }
    }
}
