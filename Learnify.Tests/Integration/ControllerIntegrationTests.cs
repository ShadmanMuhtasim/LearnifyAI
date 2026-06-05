using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Learnify.Application;
using Learnify.Application.DTOs;
using Learnify.Application.DTOs.AI;
using Learnify.Web.Controllers;
using Xunit;

namespace Learnify.Tests.Integration;

public sealed class ControllerIntegrationTests : IClassFixture<LearnifyWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LearnifyWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ControllerIntegrationTests(LearnifyWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/ai/provider")]
    [InlineData("/api/user/ai-settings")]
    [InlineData("/api/quizzes")]
    [InlineData("/api/study-planner")]
    public async Task ProtectedEndpoints_Return401WithoutJwt(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AiController_ReturnsProviderAndMockedGenerationResponses()
    {
        var token = await RegisterAndGetTokenAsync();
        using var request = NewRequest(HttpMethod.Get, "/api/ai/provider", token);

        var providerResponse = await _client.SendAsync(request);
        providerResponse.EnsureSuccessStatusCode();
        var providerJson = await providerResponse.Content.ReadAsStringAsync();

        Assert.Contains("\"provider\":\"Gemini\"", providerJson);
        Assert.Contains("gemini-3.5-flash", providerJson);

        var summaryResponse = await PostJsonAsync<SummarizeNoteResponse>(
            "/api/ai/summarize",
            new SummarizeNoteRequest("note-1", "cell biology notes"),
            token);
        Assert.Equal("Mocked integration summary.", summaryResponse.Summary);

        var flashcardResponse = await PostJsonAsync<FlashcardResponse>(
            "/api/ai/flashcards",
            new FlashcardRequest("note-1", "cell biology notes", Count: 2),
            token);
        Assert.Equal(2, flashcardResponse.Flashcards.Count);
    }

    [Fact]
    public async Task AiController_ProviderTestDistinguishesLocalOpenAiAndOllamaProtocols()
    {
        var token = await RegisterAndGetTokenAsync();

        var localOpenAi = await PostRawJsonAsync(
            "/api/ai/provider/test",
            new { provider = "LocalOpenAI", baseUrl = "http://local-openai.test", model = "local-model" },
            token);
        Assert.True(localOpenAi.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("openai", localOpenAi.RootElement.GetProperty("compatibleApi").GetString());

        var ollamaAgainstOpenAi = await PostRawJsonAsync(
            "/api/ai/provider/test",
            new { provider = "Ollama", baseUrl = "http://local-openai.test", model = "llama3" },
            token);
        Assert.False(ollamaAgainstOpenAi.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("openai", ollamaAgainstOpenAi.RootElement.GetProperty("compatibleApi").GetString());

        var ollama = await PostRawJsonAsync(
            "/api/ai/provider/test",
            new { provider = "Ollama", baseUrl = "http://ollama.test", model = "llama3" },
            token);
        Assert.True(ollama.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("ollama", ollama.RootElement.GetProperty("compatibleApi").GetString());
    }

    [Fact]
    public async Task UserAiSettingsController_SavesProvidersAndNeverReturnsRawApiKey()
    {
        var userAToken = await RegisterAndGetTokenAsync();
        var defaultSettings = await GetApiDataAsync<UserAiSettingsResponseDTO>("/api/user/ai-settings", userAToken);

        Assert.True(defaultSettings.IsDefault);
        Assert.Equal("Gemini", defaultSettings.ActiveProvider);

        var geminiResponse = await PutApiDataAsync<UserAiSettingsResponseDTO>(
            "/api/user/ai-settings",
            new
            {
                activeProvider = "Gemini",
                apiKey = "secret-gemini-key",
                customModel = "gemini-3.5-flash"
            },
            userAToken);
        Assert.Equal("Gemini", geminiResponse.ActiveProvider);

        var geminiRaw = await GetRawStringAsync("/api/user/ai-settings", userAToken);
        Assert.DoesNotContain("secret-gemini-key", geminiRaw);

        var localResponse = await PutApiDataAsync<UserAiSettingsResponseDTO>(
            "/api/user/ai-settings",
            new
            {
                activeProvider = "LocalOpenAI",
                apiKey = "local-secret",
                customModel = "Qwen3.6-35B-A3B-UD-Q4_K_M.gguf",
                localOpenAiBaseUrl = "http://127.0.0.1:8080"
            },
            userAToken);
        Assert.Equal("LocalOpenAI", localResponse.ActiveProvider);
        Assert.Equal("http://127.0.0.1:8080", localResponse.LocalOpenAiBaseUrl);

        var localRaw = await GetRawStringAsync("/api/user/ai-settings", userAToken);
        Assert.DoesNotContain("local-secret", localRaw);

        var userBToken = await RegisterAndGetTokenAsync();
        var userBSettings = await GetApiDataAsync<UserAiSettingsResponseDTO>("/api/user/ai-settings", userBToken);
        Assert.True(userBSettings.IsDefault);
        Assert.Equal("Gemini", userBSettings.ActiveProvider);
    }

    [Fact]
    public async Task QuizzesController_GeneratesScoresAndProtectsCrossUserAccess()
    {
        var ownerToken = await RegisterAndGetTokenAsync();
        var otherToken = await RegisterAndGetTokenAsync();
        var course = await CreateCourseAsync(ownerToken);
        var note = await CreateNoteAsync(ownerToken, course.Id);

        var generated = await PostApiDataAsync<QuizDto>(
            "/api/quizzes/generate",
            new GenerateQuizRequest
            {
                NoteId = note.Id,
                NumberOfQuestions = 2,
                Difficulty = "Easy",
                QuestionTypes = new List<string> { "MultipleChoice", "TrueFalse" },
                TimeLimitMinutes = 1
            },
            ownerToken,
            HttpStatusCode.Created);

        Assert.Equal(2, generated.Questions.Count);
        Assert.All(generated.Questions, question => Assert.False(string.IsNullOrWhiteSpace(question.CorrectAnswer)));

        var ownerQuizzes = await GetApiDataAsync<List<QuizDto>>("/api/quizzes", ownerToken);
        Assert.Contains(ownerQuizzes, quiz => quiz.Id == generated.Id);

        var otherQuizzes = await GetApiDataAsync<List<QuizDto>>("/api/quizzes", otherToken);
        Assert.DoesNotContain(otherQuizzes, quiz => quiz.Id == generated.Id);

        var examDetail = await GetApiDataAsync<QuizDto>($"/api/quizzes/{generated.Id}?includeAnswers=false", ownerToken);
        Assert.All(examDetail.Questions, question => Assert.Null(question.CorrectAnswer));
        Assert.All(examDetail.Questions, question => Assert.Null(question.Explanation));

        var practiceDetail = await GetApiDataAsync<QuizDto>($"/api/quizzes/{generated.Id}?includeAnswers=true", ownerToken);
        Assert.All(practiceDetail.Questions, question => Assert.False(string.IsNullOrWhiteSpace(question.CorrectAnswer)));

        var answers = practiceDetail.Questions
            .Select(question => new SubmitQuizAnswerDto
            {
                QuestionId = question.Id,
                UserAnswer = question.CorrectAnswer!
            })
            .ToList();
        var result = await PostApiDataAsync<QuizResultDto>(
            $"/api/quizzes/{generated.Id}/attempts",
            new SubmitQuizAttemptRequest { Answers = answers },
            ownerToken);

        Assert.Equal(result.TotalPoints, result.Score);
        Assert.Equal(100m, result.Percentage);

        var attempts = await GetApiDataAsync<List<QuizAttemptDto>>($"/api/quizzes/{generated.Id}/attempts", ownerToken);
        Assert.Single(attempts);

        using var crossUserRequest = NewRequest(HttpMethod.Get, $"/api/quizzes/{generated.Id}", otherToken);
        var crossUserResponse = await _client.SendAsync(crossUserRequest);
        Assert.Equal(HttpStatusCode.NotFound, crossUserResponse.StatusCode);
    }

    [Theory]
    [InlineData("learnify-note.txt", "Text upload analyze content for cells.")]
    [InlineData("learnify-note.md", "# Markdown Notes\n\nPhotosynthesis uses light.")]
    public async Task NotesAnalyzeUpload_TextAndMarkdownStillCreateNotes(
        string fileName,
        string content)
    {
        var token = await RegisterAndGetTokenAsync();
        var response = await PostApiDataAsync<AnalyzeUploadResponse>(
            "/api/notes/analyze-upload",
            new
            {
                fileName,
                fileBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
                fileType = "text",
                preferredCourseName = (string?)null
            },
            token);

        Assert.NotEqual(Guid.Empty, response.NoteId);
        var note = await GetApiDataAsync<NoteDTO>($"/api/notes/{response.NoteId}", token);
        Assert.Contains("Mocked document summary.", note.Content);
        Assert.Contains(fileName, note.Content);
    }

    [Fact]
    public async Task NotesAnalyzeUpload_TextBasedPdfExtractsReadableTextAndDoesNotStoreBase64AsContent()
    {
        var token = await RegisterAndGetTokenAsync();
        var pdfBytes = CreateTinyTextPdf("Learnify PDF extraction works for text based files.");
        var base64 = Convert.ToBase64String(pdfBytes);

        var response = await PostApiDataAsync<AnalyzeUploadResponse>(
            "/api/notes/analyze-upload",
            new
            {
                fileName = "learnify-text.pdf",
                fileBase64 = base64,
                fileType = "pdf",
                preferredCourseName = (string?)null
            },
            token);

        var note = await GetApiDataAsync<NoteDTO>($"/api/notes/{response.NoteId}", token);
        Assert.Contains("Learnify PDF extraction works", note.Content);
        Assert.DoesNotContain(base64, note.Content);
        Assert.Contains("learnify-text.pdf", note.Content);
    }

    [Fact]
    public async Task NotesAnalyzeUpload_InvalidPdfReturnsCleanBadRequest()
    {
        var token = await RegisterAndGetTokenAsync();

        using var request = NewRequest(
            HttpMethod.Post,
            "/api/notes/analyze-upload",
            token,
            new
            {
                fileName = "not-a-real.pdf",
                fileBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("not a pdf")),
                fileType = "pdf",
                preferredCourseName = (string?)null
            });
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("No readable text was extracted", body);
        Assert.DoesNotContain("Failed to analyze and save document", body);
    }

    [Fact]
    public async Task NotesUploadFile_Returns401WithoutJwt()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(Guid.NewGuid().ToString()), "courseId");
        content.Add(new ByteArrayContent(CreateTinyTextPdf("Private PDF")), "file", "private.pdf");

        var response = await _client.PostAsync("/api/notes/upload-file", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NotesUploadFile_PdfCreatesAttachmentNoteWithoutCallingAi()
    {
        _factory.ResetAiCallCounts();
        var token = await RegisterAndGetTokenAsync();
        var course = await CreateCourseAsync(token);
        var pdfBytes = CreateTinyTextPdf("Simple upload stores this PDF as an attachment.");
        var expectedBase64 = Convert.ToBase64String(pdfBytes);

        using var request = NewMultipartRequest(
            "/api/notes/upload-file",
            token,
            course.Id,
            new ByteArrayContent(pdfBytes),
            "simple-upload.pdf",
            "application/pdf");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var note = await ReadApiResponseAsync<NoteDTO>(response);
        Assert.Equal(
            "This PDF was uploaded without text extraction. Use AI Analyze on a text-based PDF or upload .txt/.md content to generate AI study tools.",
            note.Content);
        Assert.DoesNotContain(expectedBase64, note.Content);
        var attachment = Assert.Single(note.Attachments);
        Assert.Equal("simple-upload.pdf", attachment.Name);
        Assert.Equal("application/pdf", attachment.Type);
        Assert.Equal(expectedBase64, attachment.Base64);
        Assert.Equal(0, _factory.AnalyzeDocumentCallCount);

        var savedNote = await GetApiDataAsync<NoteDTO>($"/api/notes/{note.Id}", token);
        Assert.Single(savedNote.Attachments);
    }

    [Fact]
    public async Task NotesUploadFile_RejectsUnsupportedFiles()
    {
        var token = await RegisterAndGetTokenAsync();
        var course = await CreateCourseAsync(token);

        using var request = NewMultipartRequest(
            "/api/notes/upload-file",
            token,
            course.Id,
            new ByteArrayContent(Encoding.UTF8.GetBytes("not a pdf")),
            "notes.txt",
            "text/plain");
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Only PDF files are supported", body);
    }

    [Fact]
    public async Task NotesUploadFile_RejectsNonOwnedCourse()
    {
        var ownerToken = await RegisterAndGetTokenAsync();
        var otherToken = await RegisterAndGetTokenAsync();
        var ownerCourse = await CreateCourseAsync(ownerToken);

        using var request = NewMultipartRequest(
            "/api/notes/upload-file",
            otherToken,
            ownerCourse.Id,
            new ByteArrayContent(CreateTinyTextPdf("Not your course")),
            "cross-user.pdf",
            "application/pdf");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StudyPlanner_FreshUserSummaryReturnsZeros()
    {
        var token = await RegisterAndGetTokenAsync();

        var summary = await GetApiDataAsync<StudyPlanSummaryDto>("/api/study-planner/summary", token);
        var items = await GetApiDataAsync<List<StudyPlanItemDto>>("/api/study-planner", token);

        Assert.Empty(items);
        Assert.Equal(0, summary.PendingCount);
        Assert.Equal(0, summary.CompletedCount);
        Assert.Equal(0, summary.TodayCount);
        Assert.Equal(0, summary.OverdueCount);
        Assert.Equal(0, summary.TotalEstimatedMinutesToday);
        Assert.Null(summary.NextItem);
        Assert.Empty(summary.Suggestions);
    }

    [Fact]
    public async Task StudyPlanner_UserCanCreateListCompleteAndDeleteOwnItem()
    {
        var token = await RegisterAndGetTokenAsync();
        var course = await CreateCourseAsync(token);
        var note = await CreateNoteAsync(token, course.Id);

        var created = await PostApiDataAsync<StudyPlanItemDto>(
            "/api/study-planner",
            new CreateStudyPlanItemRequest
            {
                CourseId = course.Id,
                NoteId = note.Id,
                Title = "Review integration note",
                Description = "Prepare for recall practice",
                PlanType = "ReviewNote",
                ScheduledFor = DateTime.UtcNow.AddHours(2),
                EstimatedMinutes = 25,
                Priority = "High"
            },
            token,
            HttpStatusCode.Created);

        Assert.Equal("Review integration note", created.Title);
        Assert.Equal("Pending", created.Status);
        Assert.Equal(course.Id, created.CourseId);
        Assert.Equal(note.Id, created.NoteId);

        var items = await GetApiDataAsync<List<StudyPlanItemDto>>("/api/study-planner", token);
        Assert.Contains(items, item => item.Id == created.Id);

        var completed = await PostApiDataAsync<StudyPlanItemDto>(
            $"/api/study-planner/{created.Id}/complete",
            new { },
            token);

        Assert.Equal("Completed", completed.Status);
        Assert.NotNull(completed.CompletedAt);

        using var deleteRequest = NewRequest(HttpMethod.Delete, $"/api/study-planner/{created.Id}", token);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var remaining = await GetApiDataAsync<List<StudyPlanItemDto>>("/api/study-planner", token);
        Assert.DoesNotContain(remaining, item => item.Id == created.Id);
    }

    [Fact]
    public async Task StudyPlanner_ProtectsCrossUserItems()
    {
        var ownerToken = await RegisterAndGetTokenAsync();
        var otherToken = await RegisterAndGetTokenAsync();

        var created = await PostApiDataAsync<StudyPlanItemDto>(
            "/api/study-planner",
            new CreateStudyPlanItemRequest
            {
                Title = "Private study plan",
                PlanType = "Custom",
                ScheduledFor = DateTime.UtcNow.AddDays(1),
                EstimatedMinutes = 15,
                Priority = "Medium"
            },
            ownerToken,
            HttpStatusCode.Created);

        var otherItems = await GetApiDataAsync<List<StudyPlanItemDto>>("/api/study-planner", otherToken);
        Assert.DoesNotContain(otherItems, item => item.Id == created.Id);

        using var completeRequest = NewRequest(HttpMethod.Post, $"/api/study-planner/{created.Id}/complete", otherToken, new { });
        var completeResponse = await _client.SendAsync(completeRequest);
        Assert.Equal(HttpStatusCode.NotFound, completeResponse.StatusCode);

        using var deleteRequest = NewRequest(HttpMethod.Delete, $"/api/study-planner/{created.Id}", otherToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task StudyPlanner_RejectsNonOwnedCourseReference()
    {
        var ownerToken = await RegisterAndGetTokenAsync();
        var otherToken = await RegisterAndGetTokenAsync();
        var ownerCourse = await CreateCourseAsync(ownerToken);

        using var request = NewRequest(
            HttpMethod.Post,
            "/api/study-planner",
            otherToken,
            new CreateStudyPlanItemRequest
            {
                CourseId = ownerCourse.Id,
                Title = "Use someone else's course",
                PlanType = "ReviseCourse",
                ScheduledFor = DateTime.UtcNow.AddDays(1),
                EstimatedMinutes = 20,
                Priority = "Medium"
            });
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Course not found", body);
    }

    [Fact]
    public async Task StudyPlanner_SummaryReturnsUsefulSuggestionsFromNotes()
    {
        var token = await RegisterAndGetTokenAsync();
        var course = await CreateCourseAsync(token);
        await CreateNoteAsync(token, course.Id);

        var summary = await GetApiDataAsync<StudyPlanSummaryDto>("/api/study-planner/summary", token);

        Assert.Contains(summary.Suggestions, suggestion => suggestion.Title == "Review latest note");
        Assert.Contains(summary.Suggestions, suggestion => suggestion.Title == "Generate or take a quiz");
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var email = $"integration-{Guid.NewGuid():N}@learnify.test";
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterDTO
        {
            FullName = "Integration Test User",
            Email = email,
            Password = "TestPass123!",
            ConfirmPassword = "TestPass123!",
            Role = "Student"
        }, JsonOptions);
        response.EnsureSuccessStatusCode();

        var auth = await ReadApiResponseAsync<AuthResponseDTO>(response);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        return auth.Token;
    }

    private async Task<CourseDto> CreateCourseAsync(string token)
        => await PostApiDataAsync<CourseDto>(
            "/api/courses",
            new { title = "Integration Course", description = "Created by tests" },
            token,
            HttpStatusCode.Created);

    private async Task<NoteDTO> CreateNoteAsync(string token, Guid courseId)
        => await PostApiDataAsync<NoteDTO>(
            "/api/notes",
            new
            {
                courseId,
                content = "Photosynthesis and cellular respiration notes for integration quiz generation.",
                attachments = Array.Empty<NoteAttachmentDTO>()
            },
            token,
            HttpStatusCode.Created);

    private async Task<T> GetApiDataAsync<T>(string path, string token)
    {
        using var request = NewRequest(HttpMethod.Get, path, token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await ReadApiResponseAsync<T>(response);
    }

    private async Task<string> GetRawStringAsync(string path, string token)
    {
        using var request = NewRequest(HttpMethod.Get, path, token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<T> PostJsonAsync<T>(string path, object body, string token)
    {
        using var request = NewRequest(HttpMethod.Post, path, token, body);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    private async Task<JsonDocument> PostRawJsonAsync(string path, object body, string token)
    {
        using var request = NewRequest(HttpMethod.Post, path, token, body);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }

    private async Task<T> PostApiDataAsync<T>(
        string path,
        object body,
        string token,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        using var request = NewRequest(HttpMethod.Post, path, token, body);
        var response = await _client.SendAsync(request);
        Assert.Equal(expectedStatusCode, response.StatusCode);
        return await ReadApiResponseAsync<T>(response);
    }

    private async Task<T> PutApiDataAsync<T>(string path, object body, string token)
    {
        using var request = NewRequest(HttpMethod.Put, path, token, body);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await ReadApiResponseAsync<T>(response);
    }

    private static HttpRequestMessage NewRequest(
        HttpMethod method,
        string path,
        string token,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (body != null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return request;
    }

    private static HttpRequestMessage NewMultipartRequest(
        string path,
        string token,
        Guid courseId,
        ByteArrayContent fileContent,
        string fileName,
        string contentType)
    {
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        var multipart = new MultipartFormDataContent
        {
            { new StringContent(courseId.ToString()), "courseId" },
            { fileContent, "file", fileName }
        };

        var request = NewRequest(HttpMethod.Post, path, token);
        request.Content = multipart;
        return request;
    }

    private static async Task<T> ReadApiResponseAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success, apiResponse.Message);
        Assert.NotNull(apiResponse.Data);
        return apiResponse.Data;
    }

    private static byte[] CreateTinyTextPdf(string text)
    {
        static string EscapePdfText(string value)
            => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        var stream = $"BT /F1 18 Tf 72 720 Td ({EscapePdfText(text)}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream"
        };

        var builder = new StringBuilder();
        builder.Append("%PDF-1.4\n");
        var offsets = new List<int> { 0 };

        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(i + 1).Append(" 0 obj\n")
                .Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 6\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n")
            .Append(xrefOffset)
            .Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }
}
