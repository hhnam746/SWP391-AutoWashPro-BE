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
            migrationBuilder.AddColumn<Guid>(
                name: "vehicle_type_id",
                table: "vehicle",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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

            migrationBuilder.DropColumn(
                name: "vehicle_type_id",
                table: "vehicle");
        }
    }
}
