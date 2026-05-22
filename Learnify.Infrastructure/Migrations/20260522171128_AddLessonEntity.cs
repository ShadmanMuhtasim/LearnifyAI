using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Learnify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: new Guid("6b72b044-8622-4436-8135-720597afd2c1"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: new Guid("da0ae5d7-38b8-4f20-8232-87ecf536da2d"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: new Guid("fc2b412a-b9a3-49dc-8798-97cb6ebef306"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("3fe733c7-8b17-46af-acf6-e5cea979ef3b"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("82d75414-5d9f-43d0-b74d-e9d8de54014b"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("2d1e8ccd-52a3-4b92-a4d2-98dcc0057f4d"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("a423eafe-288d-445e-87f1-2f5a2a3e9fa4"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("ef527b4f-364d-45a5-b878-70bb97198bc4"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20d24c65-7736-4fe2-bfc8-013420dde884"));

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
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

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { new Guid("04a8e75c-d34a-4505-aacd-115d52ca5b00"), new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "arif@learnify.com", "Arif Hossain", "Student@123", "Student" },
                    { new Guid("ab561e92-25f7-4b5c-9349-d19de15b18f8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shadman@learnify.com", "Shadman Rahman", "Admin@123", "Admin" },
                    { new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6"), new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "fatima@learnify.com", "Fatima Akhtar", "Instructor@123", "Instructor" }
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "CreatedAt", "Description", "Title", "UserId" },
                values: new object[,]
                {
                    { new Guid("524f1e38-3c5e-4bc6-aee4-ecf81edbf4f6"), new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Build robust and scalable RESTful APIs using ASP.NET Core, including authentication, middleware, and best practices.", "ASP.NET Core Web API Development", new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6") },
                    { new Guid("8f0f0aa2-b920-4880-bbfd-9e14351a0727"), new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Deep dive into advanced C# features including async/await, reflection, expression trees, and performance optimization techniques.", "Advanced C# Programming", new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6") },
                    { new Guid("cac7a130-eb8c-4a30-b635-57254fd61e37"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Master EF Core including migrations, relationships, query tracking, change tracking, and performance tuning for enterprise applications.", "Entity Framework Core Masterclass", new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6") }
                });

            migrationBuilder.InsertData(
                table: "Notes",
                columns: new[] { "Id", "Content", "CourseId", "CreatedAt" },
                values: new object[,]
                {
                    { new Guid("1bb47140-e4b3-4009-9d3f-8be52837372a"), "EF Core Performance Tip: Use AsNoTracking() for read-only queries to avoid change tracker overhead. Profile with SQL Server Profiler.", new Guid("cac7a130-eb8c-4a30-b635-57254fd61e37"), new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("1e026033-e091-4a89-84b0-1bc7be4e00d9"), "Remember: API versioning is critical for enterprise apps. Consider URL path versioning for simplicity and HTTP header versioning for flexibility.", new Guid("524f1e38-3c5e-4bc6-aee4-ecf81edbf4f6"), new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("7563c5ce-9399-406c-8c0f-152dd7c8fc32"), "Key takeaway: Use 'await' consistently and avoid .Result to prevent deadlocks. Always prefer async all the way down.", new Guid("8f0f0aa2-b920-4880-bbfd-9e14351a0727"), new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseId",
                table: "Lessons",
                column: "CourseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: new Guid("1bb47140-e4b3-4009-9d3f-8be52837372a"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: new Guid("1e026033-e091-4a89-84b0-1bc7be4e00d9"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: new Guid("7563c5ce-9399-406c-8c0f-152dd7c8fc32"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("04a8e75c-d34a-4505-aacd-115d52ca5b00"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("ab561e92-25f7-4b5c-9349-d19de15b18f8"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("524f1e38-3c5e-4bc6-aee4-ecf81edbf4f6"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("8f0f0aa2-b920-4880-bbfd-9e14351a0727"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("cac7a130-eb8c-4a30-b635-57254fd61e37"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6"));

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { new Guid("20d24c65-7736-4fe2-bfc8-013420dde884"), new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "fatima@learnify.com", "Fatima Akhtar", "Instructor@123", "Instructor" },
                    { new Guid("3fe733c7-8b17-46af-acf6-e5cea979ef3b"), new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "arif@learnify.com", "Arif Hossain", "Student@123", "Student" },
                    { new Guid("82d75414-5d9f-43d0-b74d-e9d8de54014b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shadman@learnify.com", "Shadman Rahman", "Admin@123", "Admin" }
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "CreatedAt", "Description", "Title", "UserId" },
                values: new object[,]
                {
                    { new Guid("2d1e8ccd-52a3-4b92-a4d2-98dcc0057f4d"), new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Build robust and scalable RESTful APIs using ASP.NET Core, including authentication, middleware, and best practices.", "ASP.NET Core Web API Development", new Guid("20d24c65-7736-4fe2-bfc8-013420dde884") },
                    { new Guid("a423eafe-288d-445e-87f1-2f5a2a3e9fa4"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Master EF Core including migrations, relationships, query tracking, change tracking, and performance tuning for enterprise applications.", "Entity Framework Core Masterclass", new Guid("20d24c65-7736-4fe2-bfc8-013420dde884") },
                    { new Guid("ef527b4f-364d-45a5-b878-70bb97198bc4"), new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Deep dive into advanced C# features including async/await, reflection, expression trees, and performance optimization techniques.", "Advanced C# Programming", new Guid("20d24c65-7736-4fe2-bfc8-013420dde884") }
                });

            migrationBuilder.InsertData(
                table: "Notes",
                columns: new[] { "Id", "Content", "CourseId", "CreatedAt" },
                values: new object[,]
                {
                    { new Guid("6b72b044-8622-4436-8135-720597afd2c1"), "Remember: API versioning is critical for enterprise apps. Consider URL path versioning for simplicity and HTTP header versioning for flexibility.", new Guid("2d1e8ccd-52a3-4b92-a4d2-98dcc0057f4d"), new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("da0ae5d7-38b8-4f20-8232-87ecf536da2d"), "EF Core Performance Tip: Use AsNoTracking() for read-only queries to avoid change tracker overhead. Profile with SQL Server Profiler.", new Guid("a423eafe-288d-445e-87f1-2f5a2a3e9fa4"), new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("fc2b412a-b9a3-49dc-8798-97cb6ebef306"), "Key takeaway: Use 'await' consistently and avoid .Result to prevent deadlocks. Always prefer async all the way down.", new Guid("ef527b4f-364d-45a5-b878-70bb97198bc4"), new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }
    }
}
