using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DayScribe.Database;
using DayScribe.Database.Models;
using SmartReader;

namespace DayScribe.Services;

public class ArticleSummarizerService
{
    private readonly IDbContextFactory<DayScribeDbContext> _contextFactory;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ArticleSummarizerService> _logger;

    public ArticleSummarizerService(
        IDbContextFactory<DayScribeDbContext> contextFactory,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ArticleSummarizerService> logger)
    {
        _contextFactory = contextFactory;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ProcessPendingUrlsAsync()
    {
        _logger.LogInformation("Processing pending URLs for summarization...");

        using var db = await _contextFactory.CreateDbContextAsync();
        
        // Get already summarized URLs
        var summarizedUrls = await db.ArticleSummaries
            .Select(s => s.Url)
            .Distinct()
            .ToListAsync();

        // Get recent browser events
        var events = await db.BrowserEvents
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();

        // Filter for candidates
        var candidates = events
            .Where(e => !summarizedUrls.Contains(e.Url) && IsLongReadCandidate(e))
            // Group by URL to avoid processing duplicates in the same run
            .GroupBy(e => e.Url)
            .Select(g => g.First())
            .Take(5) // Process at most 5 articles per run to avoid spamming local LLM/APIs
            .ToList();

        _logger.LogInformation("Found {Count} candidates for summarization.", candidates.Count);

        foreach (var candidate in candidates)
        {
            try
            {
                await SummarizeUrlAsync(db, candidate.Url, candidate.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to summarize URL: {Url}", candidate.Url);
            }
        }
    }

    private bool IsLongReadCandidate(BrowserEvent ev)
    {
        if (string.IsNullOrEmpty(ev.Url)) return false;
        try
        {
            var uri = new Uri(ev.Url);
            var host = uri.Host.ToLowerInvariant();
            
            // Exclude domains that are social, media streams, search, etc.
            string[] excludedHosts = [
                "youtube.com", "youtu.be", "twitter.com", "x.com", 
                "facebook.com", "instagram.com", "linkedin.com", 
                "github.com", "reddit.com", "google.com", "bing.com",
                "localhost", "t.co", "netflix.com", "spotify.com"
            ];

            if (excludedHosts.Any(h => host.Contains(h)))
            {
                return false;
            }

            var path = uri.AbsolutePath.Trim('/');
            return path.Length > 2;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> SummarizeUrlAsync(DayScribeDbContext db, string url, string defaultTitle)
    {
        _logger.LogInformation("Downloading and reading: {Url}", url);

        // 1. Fetch & Parse article using SmartReader
        var reader = new Reader(url);
        Article article;
        try
        {
            article = await reader.GetArticleAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SmartReader failed to retrieve content for {Url}", url);
            throw;
        }

        if (article == null || string.IsNullOrWhiteSpace(article.TextContent))
        {
            throw new Exception("No readable text content extracted from the page.");
        }

        // Clean & truncate text to ~3000 tokens (approx. 12,000 characters)
        var cleanText = article.TextContent.Trim();
        if (cleanText.Length > 12000)
        {
            cleanText = cleanText.Substring(0, 12000) + "...";
        }

        var title = !string.IsNullOrEmpty(article.Title) ? article.Title : defaultTitle;

        // 2. Call LLM to summarize
        var summary = await CallLlmSummarizeAsync(cleanText);
        
        // 3. Save to database
        var summaryEntry = new ArticleSummary
        {
            Url = url,
            Title = title,
            Summary = summary,
            Date = DateTime.Today
        };

        db.ArticleSummaries.Add(summaryEntry);
        await db.SaveChangesAsync();

        _logger.LogInformation("Saved article summary for: {Url}", url);
        return summary;
    }

    private async Task<string> CallLlmSummarizeAsync(string articleText)
    {
        var prompt = $"Summarize the main learning point of this article in one clear, concise sentence:\n\n{articleText}";

        // Attempt Ollama first
        var ollamaEndpoint = _configuration.GetValue<string>("AppConfig:Ollama:Endpoint", "http://localhost:11434/api/generate");
        var ollamaModel = _configuration.GetValue<string>("AppConfig:Ollama:Model", "llama3.2:1b");

        try
        {
            _logger.LogInformation("Calling local Ollama API ({Model})...", ollamaModel);
            var response = await _httpClient.PostAsJsonAsync(ollamaEndpoint, new
            {
                model = ollamaModel,
                prompt = prompt,
                stream = false
            });

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("response", out var respProp))
                {
                    return respProp.GetString()?.Trim() ?? "No summary generated.";
                }
            }
            _logger.LogWarning("Ollama returned non-success code: {Code}. Trying fallback...", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Ollama call failed or is unavailable: {Message}. Trying fallback to OpenAI...", ex.Message);
        }

        // Fallback to OpenAI
        var openAiKey = _configuration.GetValue<string>("AppConfig:OpenAI:ApiKey");
        if (string.IsNullOrWhiteSpace(openAiKey))
        {
            throw new Exception("Ollama failed, and OpenAI API Key is not configured in appsettings.json.");
        }

        var openAiEndpoint = _configuration.GetValue<string>("AppConfig:OpenAI:Endpoint", "https://api.openai.com/v1/chat/completions");
        _logger.LogInformation("Calling OpenAI API as fallback...");

        using var request = new HttpRequestMessage(HttpMethod.Post, openAiEndpoint);
        request.Headers.Add("Authorization", $"Bearer {openAiKey}");

        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.3
        };

        request.Content = JsonContent.Create(requestBody);
        var openAiResponse = await _httpClient.SendAsync(request);

        if (!openAiResponse.IsSuccessStatusCode)
        {
            var errText = await openAiResponse.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI API call failed: {openAiResponse.StatusCode} - {errText}");
        }

        var aiJson = await openAiResponse.Content.ReadFromJsonAsync<JsonElement>();
        var content = aiJson
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content?.Trim() ?? "No summary generated.";
    }
}
