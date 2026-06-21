using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleTypeSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS vehicle_type (
                    id uuid NOT NULL DEFAULT gen_random_uuid(),
                    type_name text NOT NULL,
                    vehicle_slot integer NOT NULL,
                    size_level integer NOT NULL,
                    created_at timestamp with time zone NOT NULL DEFAULT now(),
                    updated_at timestamp with time zone NULL,
                    CONSTRAINT "PK_vehicle_type" PRIMARY KEY (id)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_vehicle_type_type_name"
                    ON vehicle_type (type_name);

                CREATE INDEX IF NOT EXISTS "IX_vehicle_type_size_level"
                    ON vehicle_type (size_level);

                INSERT INTO vehicle_type (id, type_name, vehicle_slot, size_level, created_at)
                VALUES
                    ('11111111-1111-1111-1111-111111111111', 'SUV', 12, 2, TIMESTAMPTZ '2026-06-09 21:00:00+07'),
                    ('22222222-2222-2222-2222-222222222222', 'Sedan', 5, 1, TIMESTAMPTZ '2026-06-09 21:00:00+07')
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE vehicle
                    ADD COLUMN IF NOT EXISTS vehicle_type_id uuid NULL;

                UPDATE vehicle
                SET vehicle_type_id = '22222222-2222-2222-2222-222222222222'
                WHERE vehicle_type_id IS NULL;

                ALTER TABLE vehicle
                    ALTER COLUMN vehicle_type_id SET NOT NULL;

                CREATE INDEX IF NOT EXISTS "IX_vehicle_vehicle_type_id"
                    ON vehicle (vehicle_type_id);

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_vehicle_vehicle_type_vehicle_type_id'
                    ) THEN
                        ALTER TABLE vehicle
                            ADD CONSTRAINT "FK_vehicle_vehicle_type_vehicle_type_id"
                            FOREIGN KEY (vehicle_type_id)
                            REFERENCES vehicle_type (id)
                            ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_vehicle_vehicle_type_vehicle_type_id'
                    ) THEN
                        ALTER TABLE vehicle
                            DROP CONSTRAINT "FK_vehicle_vehicle_type_vehicle_type_id";
                    END IF;
                END $$;

                DROP INDEX IF EXISTS "IX_vehicle_vehicle_type_id";

                ALTER TABLE vehicle
                    DROP COLUMN IF EXISTS vehicle_type_id;

                DROP INDEX IF EXISTS "IX_vehicle_type_size_level";
                DROP INDEX IF EXISTS "IX_vehicle_type_type_name";

                DROP TABLE IF EXISTS vehicle_type;
                """);
        }
    }
}
