using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Learnify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lessons_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notes_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "LastLoginAt", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { new Guid("04a8e75c-d34a-4505-aacd-115d52ca5b00"), new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "arif@learnify.com", "Arif Hossain", true, null, "$2a$11$KnzHt3/mqdrZR76WoxyHj.ZACNDFhncprN/5/NTdTnXszgu/X7PP2", "Student" },
                    { new Guid("ab561e92-25f7-4b5c-9349-d19de15b18f8"), new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "shadman@learnify.com", "Shadman Rahman", true, null, "$2a$11$VpBuIvUQI2kxnXE7GNiTPeiVCdDFxYV5Xl719TaA6NGqpu4nyvDIq", "Admin" },
                    { new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "fatima@learnify.com", "Fatima Akhtar", true, null, "$2a$11$rkp1IW6LlVXQ9.PlIh8MguZrnhjXvNRZ9PPOUWPm4BZ6GEKagz/2u", "Instructor" }
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "CreatedAt", "Description", "Title", "UserId" },
                values: new object[,]
                {
                    { new Guid("2daadb37-437a-45ed-bc62-d2504c1b264c"), new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Build robust and scalable RESTful APIs using ASP.NET Core, including authentication, middleware, and best practices.", "ASP.NET Core Web API Development", new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6") },
                    { new Guid("71b3c4ec-8943-40ba-b2b2-abc0eebc124a"), new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Deep dive into advanced C# features including async/await, reflection, expression trees, and performance optimization techniques.", "Advanced C# Programming", new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6") },
                    { new Guid("8a928826-6dec-4dbb-bb3f-93ab8faa4943"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Master EF Core including migrations, relationships, query tracking, change tracking, and performance tuning for enterprise applications.", "Entity Framework Core Masterclass", new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6") }
                });

            migrationBuilder.InsertData(
                table: "Notes",
                columns: new[] { "Id", "Content", "CourseId", "CreatedAt", "UserId" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "Key takeaway: Use 'await' consistently and avoid .Result to prevent deadlocks. Always prefer async all the way down.", new Guid("2daadb37-437a-45ed-bc62-d2504c1b264c"), new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"), "Remember: API versioning is critical for enterprise apps. Consider URL path versioning for simplicity and HTTP header versioning for flexibility.", new Guid("71b3c4ec-8943-40ba-b2b2-abc0eebc124a"), new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012"), "EF Core Performance Tip: Use AsNoTracking() for read-only queries to avoid change tracker overhead. Profile with SQL Server Profiler.", new Guid("8a928826-6dec-4dbb-bb3f-93ab8faa4943"), new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_UserId",
                table: "Courses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseId",
                table: "Lessons",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CourseId",
                table: "Notes",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_UserId",
                table: "Notes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
