using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPropRedemAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "branch",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RedemAmount",
                table: "booking",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_is_deleted",
                table: "branch",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking",
                columns: new[] { "branch_id", "booking_date", "start_time" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_branch_is_deleted",
                table: "branch");

            migrationBuilder.DropIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "branch");

            migrationBuilder.DropColumn(
                name: "RedemAmount",
                table: "booking");
        }
    }
}
