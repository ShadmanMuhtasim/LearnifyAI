using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAiSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AttachmentType",
                table: "Notes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AttachmentName",
                table: "Notes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[NoteAttachments]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [NoteAttachments] (
                        [Id] uniqueidentifier NOT NULL,
                        [Name] nvarchar(500) NOT NULL,
                        [Type] nvarchar(200) NOT NULL,
                        [Base64] nvarchar(max) NOT NULL,
                        [NoteId] uniqueidentifier NOT NULL,
                        CONSTRAINT [PK_NoteAttachments] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_NoteAttachments_Notes_NoteId] FOREIGN KEY ([NoteId]) REFERENCES [Notes] ([Id]) ON DELETE CASCADE
                    );
                END
                """);

            migrationBuilder.CreateTable(
                name: "UserAiSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActiveProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CustomModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OllamaBaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAiSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAiSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[NoteAttachments]', N'U') IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE name = N'IX_NoteAttachments_NoteId'
                            AND object_id = OBJECT_ID(N'[NoteAttachments]')
                    )
                BEGIN
                    CREATE INDEX [IX_NoteAttachments_NoteId] ON [NoteAttachments] ([NoteId]);
                END
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserAiSettings_UserId",
                table: "UserAiSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[NoteAttachments]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [NoteAttachments];
                END
                """);

            migrationBuilder.DropTable(
                name: "UserAiSettings");

            migrationBuilder.AlterColumn<string>(
                name: "AttachmentType",
                table: "Notes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AttachmentName",
                table: "Notes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
