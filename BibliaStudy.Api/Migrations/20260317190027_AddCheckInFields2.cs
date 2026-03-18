using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliaStudy.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckInFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Absent",
                table: "Attendances",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Present",
                table: "Attendances",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Absent",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "Present",
                table: "Attendances");
        }
    }
}
