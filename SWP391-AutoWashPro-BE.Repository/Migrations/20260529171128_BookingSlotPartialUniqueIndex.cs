using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class BookingSlotPartialUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_booking_branch_id_booking_date_start_time";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking",
                columns: new[] { "branch_id", "booking_date", "start_time" });

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX ux_booking_slot
                ON booking (branch_id, booking_date, start_time)
                WHERE status <> 'cancelled';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS ux_booking_slot;
                """);

            migrationBuilder.DropIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking");

            migrationBuilder.CreateIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking",
                columns: new[] { "branch_id", "booking_date", "start_time" },
                unique: true);
        }
    }
}
