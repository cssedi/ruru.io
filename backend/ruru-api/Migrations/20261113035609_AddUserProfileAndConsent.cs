using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ruru_api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileAndConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "MinReps",
                table: "WorkoutExercise",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "MaxReps",
                table: "WorkoutExercise",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "UserConsent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    GivenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WithdrawnAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConsent_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfile",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sex = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    MainGoal = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Height = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    CurrentWeight = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    GoalWeight = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    Pace = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    HealthConsiderations = table.Column<string[]>(type: "text[]", nullable: false),
                    ActivityLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Experience = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Sleep = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Barriers = table.Column<string[]>(type: "text[]", nullable: false),
                    TrainingLocation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TrainingDays = table.Column<string[]>(type: "text[]", nullable: false),
                    WorkoutMinutes = table.Column<int>(type: "integer", nullable: true),
                    FocusAreas = table.Column<string[]>(type: "text[]", nullable: false),
                    Diet = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ExcludedFoods = table.Column<string[]>(type: "text[]", nullable: false),
                    OnboardingCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfile", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserProfile_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserConsent_UserId_Purpose",
                table: "UserConsent",
                columns: new[] { "UserId", "Purpose" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserConsent");

            migrationBuilder.DropTable(
                name: "UserProfile");

            migrationBuilder.AlterColumn<int>(
                name: "MinReps",
                table: "WorkoutExercise",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxReps",
                table: "WorkoutExercise",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "Height",
                table: "AspNetUsers",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Weight",
                table: "AspNetUsers",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
