using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Learnify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    RequiredValue = table.Column<int>(type: "int", nullable: false),
                    AchievementType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PointsReward = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Points = table.Column<int>(type: "int", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "AchievementType", "Code", "CreatedAt", "Description", "Icon", "IsActive", "PointsReward", "RequiredValue", "Title" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "CourseCreated", "first-course", new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Create your first course.", "CR", true, 25, 1, "First Course" },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "NoteUploaded", "first-note", new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Upload or create your first note.", "NT", true, 25, 1, "First Note" },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "QuizGenerated", "first-quiz", new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Generate your first quiz.", "QZ", true, 30, 1, "First Quiz" },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "PerfectQuiz", "perfect-quiz", new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Score 100% on a quiz attempt.", "100", true, 50, 1, "Perfect Quiz" },
                    { new Guid("11111111-1111-1111-1111-111111111105"), "FlashcardsGenerated", "flashcard-starter", new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Generate flashcards from your notes.", "FC", true, 25, 1, "Flashcard Starter" },
                    { new Guid("11111111-1111-1111-1111-111111111106"), "AiToolKindsUsed", "ai-explorer", new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Use summary, flashcards, and study tips.", "AI", true, 40, 3, "AI Explorer" },
                    { new Guid("11111111-1111-1111-1111-111111111107"), "LongestStreak", "three-day-learner", new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Record learning activity on three separate days.", "ST", true, 45, 3, "Three Day Learner" },
                    { new Guid("11111111-1111-1111-1111-111111111108"), "ActivityCount", "productive-learner", new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Complete five learning actions.", "XP", true, 35, 5, "Productive Learner" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_Code",
                table: "Achievements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningActivities_UserId_OccurredAt",
                table: "LearningActivities",
                columns: new[] { "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_AchievementId",
                table: "UserAchievements",
                columns: new[] { "UserId", "AchievementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearningActivities");

            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropTable(
                name: "Achievements");
        }
    }
}
