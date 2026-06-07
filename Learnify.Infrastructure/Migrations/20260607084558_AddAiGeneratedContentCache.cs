using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiGeneratedContentCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiGeneratedContentCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GenerationMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SummaryDepth = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notice = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiGeneratedContentCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiGeneratedContentCaches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiGeneratedContentCaches_UserId_TaskType_SourceHash_Provider_Model_GenerationMode_SummaryDepth",
                table: "AiGeneratedContentCaches",
                columns: new[] { "UserId", "TaskType", "SourceHash", "Provider", "Model", "GenerationMode", "SummaryDepth" },
                unique: true,
                filter: "[SummaryDepth] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiGeneratedContentCaches");
        }
    }
}
