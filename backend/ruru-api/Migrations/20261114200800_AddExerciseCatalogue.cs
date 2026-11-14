using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ruru_api.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Equipment",
                table: "Exercise",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EquipmentGroup",
                table: "Exercise",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "Exercise",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Exercise",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Exercise",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Exercise_Name",
                table: "Exercise",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Exercise_Name",
                table: "Exercise");

            migrationBuilder.DropColumn(
                name: "Equipment",
                table: "Exercise");

            migrationBuilder.DropColumn(
                name: "EquipmentGroup",
                table: "Exercise");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Exercise");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Exercise");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Exercise");
        }
    }
}
