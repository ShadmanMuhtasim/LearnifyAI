using System.Net;
using System.Net.Http.Headers;
using Learnify.Core.Models;
using Learnify.Infrastructure.AI.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Learnify.Tests;

public class AiProviderProtocolTests
{
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
        public string RequestBody { get; private set; } = "";
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPath = request.RequestUri?.AbsolutePath ?? "";
            Authorization = request.Headers.Authorization;
            RequestBody = request.Content == null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _respond(request);
        }
    }
}
