using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Learnify.Application;
using Learnify.Application.DTOs;
using Learnify.Application.DTOs.AI;
using Xunit;

namespace Learnify.Tests.Integration;

public sealed class ControllerIntegrationTests : IClassFixture<LearnifyWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public ControllerIntegrationTests(LearnifyWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/ai/provider")]
    [InlineData("/api/user/ai-settings")]
    [InlineData("/api/quizzes")]
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

    private static async Task<T> ReadApiResponseAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success, apiResponse.Message);
        Assert.NotNull(apiResponse.Data);
        return apiResponse.Data;
    }
}
