using Learnify.Core.Interfaces;
using Learnify.Infrastructure.AI;
using Learnify.Infrastructure.AI.Providers;
using Learnify.Application.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Learnify.Infrastructure;

/// <summary>
/// Dependency injection registration for the Infrastructure layer.
/// Registers all AI providers, the factory, and the AI service.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind AI settings
        services.Configure<AiSettings>(configuration.GetSection("AiSettings"));

        // Register all AI providers as scoped
        services.AddScoped<IAiProvider, GeminiAiProvider>();
        services.AddScoped<IAiProvider, OpenAiProvider>();
        services.AddScoped<IAiProvider, OllamaAiProvider>();
        services.AddScoped<IAiProvider, LocalOpenAiProvider>();
        services.AddScoped<IAiProvider, ClaudeAiProvider>();

        // Register the factory (which also implements IAiService)
        services.AddSingleton<UserAiSettingsStore>();
        services.AddScoped<AiProviderFactory>();
        services.AddScoped<IAiService>(sp =>
            sp.GetRequiredService<AiProviderFactory>());

        // Configure HTTP clients for each AI provider
        services.AddHttpClient("GeminiClient");
        services.AddHttpClient("OpenAIClient", client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/");
        });
        services.AddHttpClient("OllamaClient", client =>
        {
            var baseUrl = configuration["AiSettings:Ollama:BaseUrl"];
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            {
                uri = new Uri("http://127.0.0.1:8080");
            }

            client.BaseAddress = uri;
        });
        services.AddHttpClient("LocalOpenAIClient", client =>
        {
            var baseUrl = configuration["AiSettings:LocalOpenAI:BaseUrl"];
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            {
                uri = new Uri("http://127.0.0.1:8080");
            }

            client.BaseAddress = uri;
        });
        services.AddHttpClient("ClaudeClient", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/");
        });

        return services;
    }
}
