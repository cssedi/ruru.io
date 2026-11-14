using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ruru_api.Migrations
{
    /// <inheritdoc />
    public partial class AddStartWith : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StartWith",
                table: "UserProfile",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartWith",
                table: "UserProfile");
        }
    }
}
