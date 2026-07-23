using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class ConvertPromotionToVoucherPersonalize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Detach the legacy Promotion relationships before renaming and reshaping
            // the existing rule table. The rule and issuance IDs must remain unchanged.
            migrationBuilder.DropForeignKey(
                name: "FK_personalized_voucher_issuance_personalized_promotion_rule_p~",
                table: "personalized_voucher_issuance");

            migrationBuilder.DropForeignKey(
                name: "FK_personalized_voucher_issuance_promotion_promotion_id",
                table: "personalized_voucher_issuance");

            migrationBuilder.DropForeignKey(
                name: "FK_personalized_promotion_rule_promotion_promotion_id",
                table: "personalized_promotion_rule");

            migrationBuilder.DropForeignKey(
                name: "FK_voucher_promotion_promotion_id",
                table: "voucher");

            migrationBuilder.DropIndex(
                name: "IX_personalized_promotion_rule_is_active_trigger_type",
                table: "personalized_promotion_rule");

            migrationBuilder.DropIndex(
                name: "IX_personalized_promotion_rule_promotion_id",
                table: "personalized_promotion_rule");

            migrationBuilder.DropIndex(
                name: "IX_personalized_promotion_rule_threshold_days",
                table: "personalized_promotion_rule");

            migrationBuilder.DropIndex(
                name: "IX_personalized_promotion_rule_trigger_type",
                table: "personalized_promotion_rule");

            migrationBuilder.DropIndex(
                name: "IX_personalized_voucher_issuance_promotion_id",
                table: "personalized_voucher_issuance");

            migrationBuilder.DropIndex(
                name: "IX_voucher_promotion_id",
                table: "voucher");

            migrationBuilder.RenameTable(
                name: "personalized_promotion_rule",
                newName: "personalized_voucher_rule");

            migrationBuilder.Sql(
                """
                ALTER TABLE personalized_voucher_rule
                RENAME CONSTRAINT "PK_personalized_promotion_rule"
                TO "PK_personalized_voucher_rule";
                """);

            migrationBuilder.RenameColumn(
                name: "promotion_rule_id",
                table: "personalized_voucher_issuance",
                newName: "voucher_rule_id");

            migrationBuilder.RenameIndex(
                name: "IX_personalized_voucher_issuance_promotion_rule_id",
                table: "personalized_voucher_issuance",
                newName: "IX_personalized_voucher_issuance_voucher_rule_id");

            // Add snapshot fields as nullable first so every historical row can be
            // backfilled before the NOT NULL constraints are enforced.
            migrationBuilder.AddColumn<string>(
                name: "voucher_name",
                table: "personalized_voucher_rule",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_type",
                table: "personalized_voucher_rule",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_value",
                table: "personalized_voucher_rule",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "voucher",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE personalized_voucher_rule AS rule
                SET voucher_name = promotion.name,
                    discount_type = promotion.discount_type,
                    discount_value = promotion.discount_value
                FROM promotion
                WHERE promotion.id = rule.promotion_id;

                UPDATE voucher AS voucher_row
                SET name = reward.name
                FROM reward
                WHERE voucher_row.reward_id = reward.id;

                UPDATE voucher AS voucher_row
                SET name = promotion.name
                FROM promotion
                WHERE voucher_row.name IS NULL
                  AND voucher_row.promotion_id = promotion.id;

                UPDATE voucher
                SET name = 'Voucher'
                WHERE name IS NULL;

                WITH ranked_active_rules AS (
                    SELECT id,
                           ROW_NUMBER() OVER (
                               PARTITION BY trigger_type
                               ORDER BY priority DESC, created_at ASC, id ASC
                           ) AS active_position
                    FROM personalized_voucher_rule
                    WHERE is_active = TRUE
                )
                UPDATE personalized_voucher_rule AS rule
                SET is_active = FALSE,
                    updated_at = now()
                FROM ranked_active_rules
                WHERE rule.id = ranked_active_rules.id
                  AND ranked_active_rules.active_position > 1;

                DO $migration_validation$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM personalized_voucher_rule
                        WHERE voucher_name IS NULL
                           OR discount_type IS NULL
                           OR discount_value IS NULL
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot convert personalized promotion rules because Promotion snapshot data is missing.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM personalized_voucher_issuance AS issuance
                        LEFT JOIN personalized_voucher_rule AS rule
                          ON rule.id = issuance.voucher_rule_id
                        WHERE rule.id IS NULL
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot convert personalized voucher issuances because a referenced rule is missing.';
                    END IF;
                END
                $migration_validation$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "voucher_name",
                table: "personalized_voucher_rule",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "discount_type",
                table: "personalized_voucher_rule",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_value",
                table: "personalized_voucher_rule",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "voucher",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            // Promotion is no longer part of the personalized voucher ownership
            // graph. Drop legacy columns only after all snapshots and references
            // have been preserved.
            migrationBuilder.DropColumn(
                name: "promotion_id",
                table: "personalized_voucher_rule");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "personalized_voucher_rule");

            migrationBuilder.DropColumn(
                name: "promotion_id",
                table: "personalized_voucher_issuance");

            migrationBuilder.DropColumn(
                name: "promotion_id",
                table: "voucher");

            migrationBuilder.CreateIndex(
                name: "IX_personalized_voucher_rule_is_active_trigger_type",
                table: "personalized_voucher_rule",
                columns: new[] { "is_active", "trigger_type" });

            migrationBuilder.CreateIndex(
                name: "IX_personalized_voucher_rule_threshold_days",
                table: "personalized_voucher_rule",
                column: "threshold_days");

            migrationBuilder.CreateIndex(
                name: "UX_personalized_voucher_rule_active_trigger",
                table: "personalized_voucher_rule",
                column: "trigger_type",
                unique: true,
                filter: "is_active = true");

            migrationBuilder.AddForeignKey(
                name: "FK_personalized_voucher_issuance_personalized_voucher_rule_vou~",
                table: "personalized_voucher_issuance",
                column: "voucher_rule_id",
                principalTable: "personalized_voucher_rule",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                INSERT INTO system_config
                    (id, config_key, config_value, description, created_at)
                VALUES
                    (
                        '9e5a54f8-1697-47c8-be81-8234b3b58e60',
                        'PersonalizedVoucher.Birthday.Enabled',
                        'true'::jsonb,
                        'Enable Birthday personalized voucher issuance.',
                        TIMESTAMPTZ '2026-05-28 20:13:39.590+07'
                    ),
                    (
                        'd9f365ae-bc06-4240-b6c4-c12a75252ea2',
                        'PersonalizedVoucher.InactiveCustomer.Enabled',
                        'true'::jsonb,
                        'Enable Inactive Customer personalized voucher issuance.',
                        TIMESTAMPTZ '2026-05-28 20:13:39.590+07'
                    ),
                    (
                        '756be207-9f48-4229-8365-974f13aafca0',
                        'PersonalizedVoucher.Welcome.Enabled',
                        'true'::jsonb,
                        'Enable Welcome personalized voucher issuance.',
                        TIMESTAMPTZ '2026-05-28 20:13:39.590+07'
                    ),
                    (
                        '2baf2ec3-21a5-46af-a917-7ed386ab9c9f',
                        'PersonalizedVoucher.NoFirstBooking.Enabled',
                        'true'::jsonb,
                        'Enable No First Booking personalized voucher issuance.',
                        TIMESTAMPTZ '2026-05-28 20:13:39.590+07'
                    ),
                    (
                        '9c0d3e70-6afe-4944-8dbf-ecee2500fd0a',
                        'PersonalizedVoucher.TierUpgrade.Enabled',
                        'true'::jsonb,
                        'Enable Tier Upgrade personalized voucher issuance.',
                        TIMESTAMPTZ '2026-05-28 20:13:39.590+07'
                    )
                ON CONFLICT (config_key) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "ConvertPromotionToVoucherPersonalize is forward-only. Restore the database backup to roll it back without losing historical Promotion mappings.");
        }
    }
}
