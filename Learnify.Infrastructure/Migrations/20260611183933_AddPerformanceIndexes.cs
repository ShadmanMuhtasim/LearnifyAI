using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quizzes_UserId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_UserId",
                table: "QuizAttempts");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "NoteAttachments",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "NoteAttachments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_UserId_CreatedAt",
                table: "Quizzes",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_UserId_CreatedAt",
                table: "QuizAttempts",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CourseId_CreatedAt",
                table: "Notes",
                columns: new[] { "CourseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CreatedAt",
                table: "Notes",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quizzes_UserId_CreatedAt",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_UserId_CreatedAt",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_Notes_CourseId_CreatedAt",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_CreatedAt",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "NoteAttachments");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "NoteAttachments");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_UserId",
                table: "Quizzes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_UserId",
                table: "QuizAttempts",
                column: "UserId");
        }
    }
}
