using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionTableFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transaction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction", x => x.id);
                    table.ForeignKey(
                        name: "FK_transaction_booking_booking_id",
                        column: x => x.booking_id,
                        principalTable: "booking",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transaction_customer_profile_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_booking_id",
                table: "transaction",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_customer_id",
                table: "transaction",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_transaction_date",
                table: "transaction",
                column: "transaction_date");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_type",
                table: "transaction",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transaction");
        }
    }
}
