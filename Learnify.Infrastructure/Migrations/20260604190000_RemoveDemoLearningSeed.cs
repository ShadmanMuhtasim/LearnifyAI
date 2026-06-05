using Learnify.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnify.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260604190000_RemoveDemoLearningSeed")]
    public partial class RemoveDemoLearningSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE qaa
                FROM QuizAttemptAnswers qaa
                INNER JOIN QuizAttempts qa ON qa.Id = qaa.QuizAttemptId
                INNER JOIN Quizzes q ON q.Id = qa.QuizId
                WHERE q.CourseId IN (
                    '2daadb37-437a-45ed-bc62-d2504c1b264c',
                    '71b3c4ec-8943-40ba-b2b2-abc0eebc124a',
                    '8a928826-6dec-4dbb-bb3f-93ab8faa4943'
                )
                OR q.NoteId IN (
                    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
                    'b2c3d4e5-f6a7-8901-bcde-f12345678901',
                    'c3d4e5f6-a7b8-9012-cdef-123456789012'
                );

                DELETE qa
                FROM QuizAttempts qa
                INNER JOIN Quizzes q ON q.Id = qa.QuizId
                WHERE q.CourseId IN (
                    '2daadb37-437a-45ed-bc62-d2504c1b264c',
                    '71b3c4ec-8943-40ba-b2b2-abc0eebc124a',
                    '8a928826-6dec-4dbb-bb3f-93ab8faa4943'
                )
                OR q.NoteId IN (
                    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
                    'b2c3d4e5-f6a7-8901-bcde-f12345678901',
                    'c3d4e5f6-a7b8-9012-cdef-123456789012'
                );

                DELETE qu
                FROM Questions qu
                INNER JOIN Quizzes q ON q.Id = qu.QuizId
                WHERE q.CourseId IN (
                    '2daadb37-437a-45ed-bc62-d2504c1b264c',
                    '71b3c4ec-8943-40ba-b2b2-abc0eebc124a',
                    '8a928826-6dec-4dbb-bb3f-93ab8faa4943'
                )
                OR q.NoteId IN (
                    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
                    'b2c3d4e5-f6a7-8901-bcde-f12345678901',
                    'c3d4e5f6-a7b8-9012-cdef-123456789012'
                );

                DELETE FROM Quizzes
                WHERE CourseId IN (
                    '2daadb37-437a-45ed-bc62-d2504c1b264c',
                    '71b3c4ec-8943-40ba-b2b2-abc0eebc124a',
                    '8a928826-6dec-4dbb-bb3f-93ab8faa4943'
                )
                OR NoteId IN (
                    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
                    'b2c3d4e5-f6a7-8901-bcde-f12345678901',
                    'c3d4e5f6-a7b8-9012-cdef-123456789012'
                );

                DELETE FROM NoteAttachments
                WHERE NoteId IN (
                    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
                    'b2c3d4e5-f6a7-8901-bcde-f12345678901',
                    'c3d4e5f6-a7b8-9012-cdef-123456789012'
                );

                DELETE FROM Notes
                WHERE Id IN (
                    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
                    'b2c3d4e5-f6a7-8901-bcde-f12345678901',
                    'c3d4e5f6-a7b8-9012-cdef-123456789012'
                );

                DELETE FROM Courses
                WHERE Id IN (
                    '2daadb37-437a-45ed-bc62-d2504c1b264c',
                    '71b3c4ec-8943-40ba-b2b2-abc0eebc124a',
                    '8a928826-6dec-4dbb-bb3f-93ab8faa4943'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Demo learning seed data is intentionally not restored.
        }
    }
}
