using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldSePayWebhook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "account_number",
                table: "transaction",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_reference_code",
                table: "transaction",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expired_at",
                table: "transaction",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_transaction_id",
                table: "transaction",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gateway",
                table: "transaction",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "paid_at",
                table: "transaction",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider",
                table: "transaction",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_code",
                table: "transaction",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_description",
                table: "transaction",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "provider_transaction_date",
                table: "transaction",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "raw_content",
                table: "transaction",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "raw_payload",
                table: "transaction",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_code",
                table: "transaction",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "transaction",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transfer_type",
                table: "transaction",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "wallet_balance_after",
                table: "transaction",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "wallet_balance_before",
                table: "transaction",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_external_transaction_id",
                table: "transaction",
                column: "external_transaction_id",
                unique: true,
                filter: "external_transaction_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_paid_at",
                table: "transaction",
                column: "paid_at");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_reference_code",
                table: "transaction",
                column: "reference_code",
                unique: true,
                filter: "reference_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_status",
                table: "transaction",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transaction_external_transaction_id",
                table: "transaction");

            migrationBuilder.DropIndex(
                name: "IX_transaction_paid_at",
                table: "transaction");

            migrationBuilder.DropIndex(
                name: "IX_transaction_reference_code",
                table: "transaction");

            migrationBuilder.DropIndex(
                name: "IX_transaction_status",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "account_number",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "bank_reference_code",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "expired_at",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "external_transaction_id",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "gateway",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "paid_at",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "provider",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "provider_code",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "provider_description",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "provider_transaction_date",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "raw_content",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "raw_payload",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "reference_code",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "status",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "transfer_type",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "wallet_balance_after",
                table: "transaction");

            migrationBuilder.DropColumn(
                name: "wallet_balance_before",
                table: "transaction");
        }
    }
}
