using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking");

            migrationBuilder.AddColumn<Guid>(
                name: "vehicle_type_id",
                table: "vehicle",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "user",
                type: "text",
                nullable: false,
                defaultValue: "pending",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "active");

            migrationBuilder.Sql("""
                ALTER TABLE "user" ADD COLUMN IF NOT EXISTS "Reason" text;
            """);

            migrationBuilder.CreateTable(
                name: "vehicle_image",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_image", x => x.id);
                    table.ForeignKey(
                        name: "FK_vehicle_image_vehicle_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_type",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    type_name = table.Column<string>(type: "text", nullable: false),
                    vehicle_slot = table.Column<int>(type: "integer", nullable: false),
                    size_level = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_type", x => x.id);
                });

            migrationBuilder.Sql("""
                INSERT INTO system_config (id, config_key, config_value, created_at, description)
                VALUES
                    ('09f7cba0-c348-4654-90d6-bdd3b21385fa', 'BonusPoint', '10', '2026-05-28T20:13:39.59+07:00', 'Bonus points earned after checkout completed.'),
                    ('219a17c5-c218-4c0c-b0e0-6e95fd0c6b11', 'PaymentDeposite', '30', '2026-05-28T20:13:39.59+07:00', 'Deposit percentage required for booking.'),
                    ('490a0d6b-e4ca-4315-a387-b92b6f52c9bc', 'SlotDurationMinutes', '15', '2026-05-28T20:13:39.59+07:00', 'Duration of each booking slot in minutes.'),
                    ('6e830ac7-1934-4392-b05a-b4f777302170', 'WorkingStartHour', '8', '2026-05-28T20:13:39.59+07:00', 'Default working start time in Vietnam timezone UTC+7.'),
                    ('8b2e6d2b-0c74-47a0-9c5f-9d83029de001', 'SuvBasePrice', '30000', '2026-05-28T20:13:39.59+07:00', 'Additional base price for SUV vehicles.'),
                    ('8d456f5d-26ba-45f1-a57f-d88234758685', 'WorkingEndHour', '17', '2026-05-28T20:13:39.59+07:00', 'Default working end time in Vietnam timezone UTC+7.'),
                    ('f1a24c4e-1978-4db9-8d6a-2cb7a3f7f002', 'SedanBasePrice', '0', '2026-05-28T20:13:39.59+07:00', 'Additional base price for Sedan vehicles.'),
                    ('f96ce391-eb3a-4a8e-ad76-18c3f8da6668', 'BasePrice', '100000', '2026-05-28T20:13:39.59+07:00', 'Default base price for service.')
                ON CONFLICT (id) DO NOTHING;
            """);

            migrationBuilder.InsertData(
                table: "vehicle_type",
                columns: new[] { "id", "created_at", "size_level", "type_name", "updated_at", "vehicle_slot" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 6, 9, 21, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)), 2, "SUV", null, 12 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTimeOffset(new DateTime(2026, 6, 9, 21, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0)), 1, "Sedan", null, 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_vehicle_type_id",
                table: "vehicle",
                column: "vehicle_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking",
                columns: new[] { "branch_id", "booking_date", "start_time" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_image_vehicle_id",
                table: "vehicle_image",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_type_size_level",
                table: "vehicle_type",
                column: "size_level");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_type_type_name",
                table: "vehicle_type",
                column: "type_name",
                unique: true);

            migrationBuilder.Sql("""
                UPDATE vehicle
                SET vehicle_type_id = '22222222-2222-2222-2222-222222222222'
                WHERE vehicle_type_id = '00000000-0000-0000-0000-000000000000';
            """);

            migrationBuilder.AddForeignKey(
                name: "FK_vehicle_vehicle_type_vehicle_type_id",
                table: "vehicle",
                column: "vehicle_type_id",
                principalTable: "vehicle_type",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vehicle_vehicle_type_vehicle_type_id",
                table: "vehicle");

            migrationBuilder.DropTable(
                name: "vehicle_image");

            migrationBuilder.DropTable(
                name: "vehicle_type");

            migrationBuilder.DropIndex(
                name: "IX_vehicle_vehicle_type_id",
                table: "vehicle");

            migrationBuilder.DropIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking");

            migrationBuilder.Sql("""
                DELETE FROM system_config WHERE id IN (
                    '09f7cba0-c348-4654-90d6-bdd3b21385fa',
                    '219a17c5-c218-4c0c-b0e0-6e95fd0c6b11',
                    '490a0d6b-e4ca-4315-a387-b92b6f52c9bc',
                    '6e830ac7-1934-4392-b05a-b4f777302170',
                    '8b2e6d2b-0c74-47a0-9c5f-9d83029de001',
                    '8d456f5d-26ba-45f1-a57f-d88234758685',
                    'f1a24c4e-1978-4db9-8d6a-2cb7a3f7f002',
                    'f96ce391-eb3a-4a8e-ad76-18c3f8da6668'
                );
            """);

            migrationBuilder.DropColumn(
                name: "vehicle_type_id",
                table: "vehicle");

            migrationBuilder.Sql("""
                ALTER TABLE "user" DROP COLUMN IF EXISTS "Reason";
            """);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "user",
                type: "text",
                nullable: false,
                defaultValue: "active",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "pending");

            migrationBuilder.CreateIndex(
                name: "IX_booking_branch_id_booking_date_start_time",
                table: "booking",
                columns: new[] { "branch_id", "booking_date", "start_time" },
                unique: true);
        }
    }
}
