using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliaStudy.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSetHighlightedBlog : Migration
    {
        /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
{
    // Só tenta remover se existir
    migrationBuilder.Sql(@"
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_name='Attendances' AND column_name='Absent'
            ) THEN
                ALTER TABLE ""Attendances"" DROP COLUMN ""Absent"";
            END IF;
        END
        $$;
    ");

    migrationBuilder.AddColumn<bool>(
        name: "IsHighlighted",
        table: "Notes",
        type: "boolean",
        nullable: false,
        defaultValue: false);
}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHighlighted",
                table: "Notes");

            migrationBuilder.AddColumn<bool>(
                name: "Absent",
                table: "Attendances",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}