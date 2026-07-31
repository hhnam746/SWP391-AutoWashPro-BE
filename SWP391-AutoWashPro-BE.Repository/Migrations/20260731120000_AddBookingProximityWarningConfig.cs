using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260731120000_AddBookingProximityWarningConfig")]
public partial class AddBookingProximityWarningConfig : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO system_config (
                id,
                config_key,
                config_value,
                description,
                created_at)
            VALUES (
                '42e79cb2-b580-47b4-9ba8-93c5dfd05666',
                'BookingProximityWarningMinutes',
                '30'::jsonb,
                'Maximum gap in minutes between a customer''s bookings before confirmation is required.',
                TIMESTAMPTZ '2026-05-28 13:13:39.590+00')
            ON CONFLICT (config_key) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM system_config
            WHERE id = '42e79cb2-b580-47b4-9ba8-93c5dfd05666'
              AND config_key = 'BookingProximityWarningMinutes';
            """);
    }
}
