using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserStatusDefaultToPending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "user",
                type: "text",
                nullable: false,
                defaultValue: "pending",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "user",
                type: "text",
                nullable: false,
                defaultValue: "active",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "pending");
        }
    }
}
