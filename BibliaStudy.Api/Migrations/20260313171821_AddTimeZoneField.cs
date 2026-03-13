using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliaStudy.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeZoneField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Users");
        }
    }
}
