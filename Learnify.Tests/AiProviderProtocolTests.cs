using System.Net;
using System.Net.Http.Headers;
using Learnify.Application.Settings;
using Learnify.Core.Models;
using Learnify.Infrastructure.AI;
using Learnify.Infrastructure.AI.Providers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Learnify.Tests;

public class AiProviderProtocolTests
{
    [Fact]
    public async Task GeminiProvider_MissingApiKeyThrowsClearConfigurationError()
    {
        var provider = new GeminiAiProvider(
            new StaticHttpClientFactory(new HttpClient()),
            Options.Create(new AiSettings { Gemini = new AiSettings.GeminiSettings { ApiKey = "", Model = "gemini-3.5-flash" } }),
            NullLogger<GeminiAiProvider>.Instance);

        var ex = await Assert.ThrowsAsync<AiProviderException>(() =>
            provider.CompleteAsync("Prompt", new AiRequestOptions(), CancellationToken.None));

        Assert.Equal("AI_CONFIG_MISSING", ex.Code);
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("Gemini API key is not configured", ex.Message);
        Assert.Contains("user secrets or Settings", ex.Message);
    }

    [Fact]
    public async Task GeminiProvider_PostsGenerateContentAndParsesPlainText()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {"candidates":[{"content":{"parts":[{"text":"plain summary"}]}}]}
            """)
        });
        var provider = new GeminiAiProvider(
            new StaticHttpClientFactory(new HttpClient(handler)),
            Options.Create(new AiSettings { Gemini = new AiSettings.GeminiSettings { ApiKey = "gemini-key", Model = "gemini-3.5-flash" } }),
            NullLogger<GeminiAiProvider>.Instance);

        var result = await provider.CompleteAsync("Summarize this.", new AiRequestOptions(0.2, 123), CancellationToken.None);

        Assert.Equal("plain summary", result);
        Assert.Contains("/v1beta/models/gemini-3.5-flash:generateContent", handler.RequestPath);
        Assert.Contains("key=gemini-key", handler.RequestQuery);
        Assert.Contains("maxOutputTokens", handler.RequestBody);
    }

    [Fact]
    public async Task GeminiProvider_HttpErrorIncludesSafeProviderReason()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"error":{"message":"models/gemini-3.5-flash is not found for API version v1beta"}}""")
        });
        var provider = new GeminiAiProvider(
            new StaticHttpClientFactory(new HttpClient(handler)),
            Options.Create(new AiSettings { Gemini = new AiSettings.GeminiSettings { ApiKey = "gemini-key", Model = "gemini-3.5-flash" } }),
            NullLogger<GeminiAiProvider>.Instance);

        var ex = await Assert.ThrowsAsync<AiProviderException>(() =>
            provider.CompleteAsync("Prompt", new AiRequestOptions(), CancellationToken.None));

        Assert.Equal("AI_PROVIDER_UNAVAILABLE", ex.Code);
        Assert.Equal(502, ex.StatusCode);
        Assert.Contains("Gemini provider model or endpoint was not found", ex.Message);
        Assert.Contains("Check AI provider settings", ex.Message);
    }

    [Fact]
    public async Task GeminiProvider_EmptyCandidateTextThrowsClearProviderError()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"candidates":[{"finishReason":"MAX_TOKENS","content":{"parts":[{"text":""}]}}]}""")
        });
        var provider = new GeminiAiProvider(
            new StaticHttpClientFactory(new HttpClient(handler)),
            Options.Create(new AiSettings { Gemini = new AiSettings.GeminiSettings { ApiKey = "gemini-key", Model = "gemini-3.5-flash" } }),
            NullLogger<GeminiAiProvider>.Instance);

        var ex = await Assert.ThrowsAsync<AiProviderException>(() =>
            provider.CompleteAsync("Prompt", new AiRequestOptions(), CancellationToken.None));

        Assert.Equal("AI_PROVIDER_UNAVAILABLE", ex.Code);
        Assert.Equal(502, ex.StatusCode);
        Assert.Contains("Gemini provider returned no text", ex.Message);
        Assert.Contains("MAX_TOKENS", ex.Message);
    }

    [Fact]
    public void FlashcardParser_AcceptsFencedJsonWithLeadingText()
    {
        var flashcards = AiProviderFactory.ParseFlashcards("""
        Here are the cards:
        ```json
        [
          { "question": "What is a stack?", "answer": "A LIFO data structure." },
          { "term": "Queue", "definition": "A FIFO data structure." }
        ]
        ```
        """);

        Assert.Equal(2, flashcards.Count);
        Assert.Equal("What is a stack?", flashcards[0].Question);
        Assert.Equal("A FIFO data structure.", flashcards[1].Answer);
    }

    [Fact]
    public async Task LocalOpenAiProvider_PostsToChatCompletionsAndParsesContent()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {"choices":[{"message":{"content":"local summary"}}]}
            """)
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        var provider = new LocalOpenAiProvider(
            client,
            "Qwen3.6-35B-A3B-UD-Q4_K_M.gguf",
            "",
            NullLogger<LocalOpenAiProvider>.Instance);

        var result = await provider.CompleteAsync("Summarize this.", new AiRequestOptions(0.2, 123), CancellationToken.None);

        Assert.Equal("local summary", result);
        Assert.Equal("/v1/chat/completions", handler.RequestPath);
        Assert.Contains("Qwen3.6-35B-A3B-UD-Q4_K_M.gguf", handler.RequestBody);
        Assert.Null(handler.Authorization);
        Assert.DoesNotContain("/api/generate", handler.RequestPath);
    }

    [Fact]
    public async Task LocalOpenAiProvider_SendsBearerTokenOnlyWhenConfigured()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"choices":[{"message":{"content":"ok"}}]}""")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        var provider = new LocalOpenAiProvider(
            client,
            "local-model",
            "local-secret",
            NullLogger<LocalOpenAiProvider>.Instance);

        await provider.CompleteAsync("Prompt", new AiRequestOptions(), CancellationToken.None);

        Assert.Equal("Bearer", handler.Authorization?.Scheme);
        Assert.Equal("local-secret", handler.Authorization?.Parameter);
    }

    [Fact]
    public async Task LocalOpenAiProvider_MalformedResponseThrowsClearException()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"choices":[{"message":{}}]}""")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        var provider = new LocalOpenAiProvider(
            client,
            "local-model",
            "",
            NullLogger<LocalOpenAiProvider>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CompleteAsync("Prompt", new AiRequestOptions(), CancellationToken.None));

        Assert.Contains("LocalOpenAI", ex.Message);
    }

    [Fact]
    public async Task LocalOpenAiProvider_EmptyContentThrowsClearException()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {"choices":[{"finish_reason":"length","message":{"content":"","reasoning_content":"thinking only"}}]}
            """)
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        var provider = new LocalOpenAiProvider(
            client,
            "local-model",
            "",
            NullLogger<LocalOpenAiProvider>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CompleteAsync("Prompt", new AiRequestOptions(), CancellationToken.None));

        Assert.Contains("choices[0].message.content", ex.Message);
    }

    [Fact]
    public async Task OllamaProvider_UsesOllamaGenerateEndpointNotOpenAiChatEndpoint()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"response":"ollama summary"}""")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var provider = new OllamaAiProvider(
            client,
            "llama3",
            NullLogger<OllamaAiProvider>.Instance);

        var result = await provider.CompleteAsync("Summarize this.", new AiRequestOptions(0.2, 100), CancellationToken.None);

        Assert.Equal("ollama summary", result);
        Assert.Equal("/api/generate", handler.RequestPath);
        Assert.Contains("llama3", handler.RequestBody);
        Assert.DoesNotContain("/v1/chat/completions", handler.RequestPath);
    }

    [Fact]
    public async Task OllamaProvider_NonOllamaServerFailureIncludesStatusAndBody()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"error":{"message":"File Not Found"}}""")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        var provider = new OllamaAiProvider(
            client,
            "llama3",
            NullLogger<OllamaAiProvider>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CompleteAsync("Prompt", new AiRequestOptions(), CancellationToken.None));

        Assert.Equal("/api/generate", handler.RequestPath);
        Assert.Contains("NotFound", ex.Message);
        Assert.Contains("File Not Found", ex.Message);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        public string RequestPath { get; private set; } = "";
        public string RequestQuery { get; private set; } = "";
        public string RequestBody { get; private set; } = "";
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPath = request.RequestUri?.AbsolutePath ?? "";
            RequestQuery = request.RequestUri?.Query ?? "";
            Authorization = request.Headers.Authorization;
            RequestBody = request.Content == null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _respond(request);
        }
    }

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public StaticHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpClient CreateClient(string name) => _httpClient;
    }
}
