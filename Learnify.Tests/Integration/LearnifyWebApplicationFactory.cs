using System.Net;
using Learnify.Application.Settings;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Learnify.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Learnify.Tests.Integration;

public sealed class LearnifyWebApplicationFactory : WebApplicationFactory<global::Program>
{
    private readonly string _databaseName = $"learnify-tests-{Guid.NewGuid()}";
    private int _analyzeDocumentCallCount;
    private int _summaryCallCount;
    private int _flashcardCallCount;
    private int _studyTipsCallCount;

    public int AnalyzeDocumentCallCount => _analyzeDocumentCallCount;
    public int SummaryCallCount => _summaryCallCount;
    public int FlashcardCallCount => _flashcardCallCount;
    public int StudyTipsCallCount => _studyTipsCallCount;
    public bool ShouldFailStudyGeneration { get; set; }

    public void ResetAiCallCounts()
    {
        _analyzeDocumentCallCount = 0;
        _summaryCallCount = 0;
        _flashcardCallCount = 0;
        _studyTipsCallCount = 0;
        ShouldFailStudyGeneration = false;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=LearnifyTests;Trusted_Connection=True;",
                ["JwtSettings:SecretKey"] = "integration-test-secret-key-with-enough-length",
                ["JwtSettings:Issuer"] = "LearnifyAPI",
                ["JwtSettings:Audience"] = "LearnifyClients",
                ["JwtSettings:ExpirationMinutes"] = "60",
                ["AiSettings:ActiveProvider"] = "Gemini",
                ["AiSettings:Gemini:ApiKey"] = "default-gemini-key",
                ["AiSettings:Gemini:Model"] = "gemini-3.5-flash",
                ["AiSettings:LocalOpenAI:BaseUrl"] = "http://127.0.0.1:8080",
                ["AiSettings:LocalOpenAI:Model"] = "Qwen3.6-35B-A3B-UD-Q4_K_M.gguf",
                ["AiSettings:Ollama:BaseUrl"] = "http://127.0.0.1:8080",
                ["AiSettings:Ollama:Model"] = "llama3"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IAiService>();
            services.RemoveAll<IHttpClientFactory>();
            services.RemoveAll<IHostedService>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
            services.AddScoped<IAiService>(_ => new FakeAiService(this));
            services.AddSingleton<IHttpClientFactory, FakeLocalProtocolHttpClientFactory>();

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        });
    }

    private sealed class FakeAiService : IAiService
    {
        private readonly LearnifyWebApplicationFactory _factory;

        public FakeAiService(LearnifyWebApplicationFactory factory)
        {
            _factory = factory;
        }

        public Task<string> SummarizeNoteAsync(string content, CancellationToken ct = default)
        {
            Interlocked.Increment(ref _factory._summaryCallCount);
            if (_factory.ShouldFailStudyGeneration)
            {
                throw new InvalidOperationException("AI_PROVIDER_UNAVAILABLE: mocked provider failure.");
            }

            return Task.FromResult("Mocked integration summary.");
        }

        public Task<IReadOnlyList<FlashcardResult>> GenerateFlashcardsAsync(
            string content,
            int count = 5,
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref _factory._flashcardCallCount);
            if (_factory.ShouldFailStudyGeneration)
            {
                throw new InvalidOperationException("AI_PROVIDER_UNAVAILABLE: mocked provider failure.");
            }

            IReadOnlyList<FlashcardResult> cards = Enumerable.Range(1, Math.Max(1, count))
                .Select(i => new FlashcardResult($"Question {i}?", $"Answer {i}"))
                .ToList();
            return Task.FromResult(cards);
        }

        public Task<GeneratedQuizResult> GenerateQuizAsync(
            string content,
            IReadOnlyList<string> questionTypes,
            string difficulty,
            int numberOfQuestions,
            CancellationToken ct = default)
        {
            var requestedTypes = questionTypes.Count == 0
                ? new[] { "MultipleChoice" }
                : questionTypes;
            var questions = new List<GeneratedQuizQuestionResult>();

            for (var i = 0; i < Math.Max(1, numberOfQuestions); i++)
            {
                var type = requestedTypes[i % requestedTypes.Count];
                questions.Add(type.Equals("TrueFalse", StringComparison.OrdinalIgnoreCase)
                    ? new GeneratedQuizQuestionResult
                    {
                        Type = "TrueFalse",
                        QuestionText = $"True or false question {i + 1}?",
                        Options = new List<string> { "True", "False" },
                        CorrectAnswer = "True",
                        Explanation = "Mocked true/false explanation."
                    }
                    : new GeneratedQuizQuestionResult
                    {
                        Type = "MultipleChoice",
                        QuestionText = $"Multiple choice question {i + 1}?",
                        Options = new List<string> { "A", "B", "C", "D" },
                        CorrectAnswer = "B",
                        Explanation = "Mocked multiple-choice explanation."
                    });
            }

            return Task.FromResult(new GeneratedQuizResult
            {
                Title = "Mocked Integration Quiz",
                Questions = questions
            });
        }

        public Task<string> GetStudyTipsAsync(string topic, CancellationToken ct = default)
        {
            Interlocked.Increment(ref _factory._studyTipsCallCount);
            if (_factory.ShouldFailStudyGeneration)
            {
                throw new InvalidOperationException("AI_PROVIDER_UNAVAILABLE: mocked provider failure.");
            }

            return Task.FromResult("Mocked study tips.");
        }

        public Task<NoteAnalysisResult> AnalyzeDocumentAsync(
            string content,
            string fileName,
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref _factory._analyzeDocumentCallCount);

            return Task.FromResult(new NoteAnalysisResult
            {
                SuggestedCourseName = "Mocked Course",
                Summary = "Mocked document summary.",
                DetectedTopics = new List<string> { "Mocked Topic" },
                ExtractedText = content
            });
        }
    }

    private sealed class FakeLocalProtocolHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new FakeLocalProtocolHandler());
    }

    private sealed class FakeLocalProtocolHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var host = request.RequestUri?.Host ?? "";
            var path = request.RequestUri?.AbsolutePath ?? "";
            var isOpenAiHost = host.Contains("local-openai", StringComparison.OrdinalIgnoreCase);
            var isOllamaHost = host.Contains("ollama", StringComparison.OrdinalIgnoreCase);
            var ok = (isOpenAiHost && path == "/v1/models") ||
                     (isOllamaHost && path == "/api/tags");

            var response = new HttpResponseMessage(ok ? HttpStatusCode.OK : HttpStatusCode.NotFound)
            {
                Content = new StringContent(ok ? "{}" : "{\"error\":\"not found\"}")
            };
            return Task.FromResult(response);
        }
    }
}
