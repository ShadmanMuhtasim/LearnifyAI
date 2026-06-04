using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Learnify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("2daadb37-437a-45ed-bc62-d2504c1b264c"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("71b3c4ec-8943-40ba-b2b2-abc0eebc124a"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("8a928826-6dec-4dbb-bb3f-93ab8faa4943"));

            migrationBuilder.AddColumn<string>(
                name: "AttachmentBase64",
                table: "Notes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentName",
                table: "Notes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentType",
                table: "Notes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "CreatedAt", "Description", "Title", "UserId" },
                values: new object[,]
                {
                    { new Guid("230961ab-ea6c-4aba-b34f-702ee6be33ad"), new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Build robust and scalable RESTful APIs using ASP.NET Core, including authentication, middleware, and best practices.", "ASP.NET Core Web API Development", new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6") },
                    { new Guid("3758580a-e8c8-4208-b51f-8cc5561609ad"), new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Deep dive into advanced C# features including async/await, reflection, expression trees, and performance optimization techniques.", "Advanced C# Programming", new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6") },
                    { new Guid("50739146-eff4-4fc5-befc-7f93307799a9"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Master EF Core including migrations, relationships, query tracking, change tracking, and performance tuning for enterprise applications.", "Entity Framework Core Masterclass", new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6") }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("04a8e75c-d34a-4505-aacd-115d52ca5b00"),
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "$2a$11$nEaM7wvmn2IjWkyvfw3m5eF4UmtNCOjNtB0AfeH1GtKNdLO/y3g3C" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("ab561e92-25f7-4b5c-9349-d19de15b18f8"),
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "$2a$11$lqLEXMuNVPmeo9sOpXIIfuCuJD0TtVSM65/IrjohLk1blGjblEUZC" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6"),
                column: "PasswordHash",
                value: "$2a$11$f5NYkhm9RA0scsEEhwlK2OAuozIv2nG4en571KfDQGczKY9W.X8Lq");

            migrationBuilder.InsertData(
                table: "Notes",
                columns: new[] { "Id", "AttachmentBase64", "AttachmentName", "AttachmentType", "Content", "CourseId", "CreatedAt", "UserId" },
                values: new object[,]
                {
                    { new Guid("272e39f6-c483-411b-b774-5ff15fc46409"), null, null, null, "EF Core Performance Tip: Use AsNoTracking() for read-only queries to avoid change tracker overhead. Profile with SQL Server Profiler.", new Guid("50739146-eff4-4fc5-befc-7f93307799a9"), new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("2e5d97cc-4bc3-43b4-8962-163ac2c73194"), null, null, null, "Remember: API versioning is critical for enterprise apps. Consider URL path versioning for simplicity and HTTP header versioning for flexibility.", new Guid("230961ab-ea6c-4aba-b34f-702ee6be33ad"), new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("713a0006-3abf-42b1-9615-b8f370b7b1ba"), null, null, null, "Key takeaway: Use 'await' consistently and avoid .Result to prevent deadlocks. Always prefer async all the way down.", new Guid("3758580a-e8c8-4208-b51f-8cc5561609ad"), new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: new Guid("272e39f6-c483-411b-b774-5ff15fc46409"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: new Guid("2e5d97cc-4bc3-43b4-8962-163ac2c73194"));

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: new Guid("713a0006-3abf-42b1-9615-b8f370b7b1ba"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("230961ab-ea6c-4aba-b34f-702ee6be33ad"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("3758580a-e8c8-4208-b51f-8cc5561609ad"));

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("50739146-eff4-4fc5-befc-7f93307799a9"));

            migrationBuilder.DropColumn(
                name: "AttachmentBase64",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "AttachmentName",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "AttachmentType",
                table: "Notes");

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "CreatedAt", "Description", "Title", "UserId" },
                values: new object[,]
                {
                    { new Guid("2daadb37-437a-45ed-bc62-d2504c1b264c"), new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Build robust and scalable RESTful APIs using ASP.NET Core, including authentication, middleware, and best practices.", "ASP.NET Core Web API Development", new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6") },
                    { new Guid("71b3c4ec-8943-40ba-b2b2-abc0eebc124a"), new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Deep dive into advanced C# features including async/await, reflection, expression trees, and performance optimization techniques.", "Advanced C# Programming", new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6") },
                    { new Guid("8a928826-6dec-4dbb-bb3f-93ab8faa4943"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Master EF Core including migrations, relationships, query tracking, change tracking, and performance tuning for enterprise applications.", "Entity Framework Core Masterclass", new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6") }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("04a8e75c-d34a-4505-aacd-115d52ca5b00"),
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "$2a$11$KnzHt3/mqdrZR76WoxyHj.ZACNDFhncprN/5/NTdTnXszgu/X7PP2" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("ab561e92-25f7-4b5c-9349-d19de15b18f8"),
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "$2a$11$VpBuIvUQI2kxnXE7GNiTPeiVCdDFxYV5Xl719TaA6NGqpu4nyvDIq" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b135b12a-f7a7-45b7-8de2-1acd495220c6"),
                column: "PasswordHash",
                value: "$2a$11$rkp1IW6LlVXQ9.PlIh8MguZrnhjXvNRZ9PPOUWPm4BZ6GEKagz/2u");

            migrationBuilder.InsertData(
                table: "Notes",
                columns: new[] { "Id", "Content", "CourseId", "CreatedAt", "UserId" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "Key takeaway: Use 'await' consistently and avoid .Result to prevent deadlocks. Always prefer async all the way down.", new Guid("2daadb37-437a-45ed-bc62-d2504c1b264c"), new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"), "Remember: API versioning is critical for enterprise apps. Consider URL path versioning for simplicity and HTTP header versioning for flexibility.", new Guid("71b3c4ec-8943-40ba-b2b2-abc0eebc124a"), new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012"), "EF Core Performance Tip: Use AsNoTracking() for read-only queries to avoid change tracker overhead. Profile with SQL Server Profiler.", new Guid("8a928826-6dec-4dbb-bb3f-93ab8faa4943"), new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });
        }
    }
}
