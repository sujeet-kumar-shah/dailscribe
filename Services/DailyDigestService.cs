using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DayScribe.Database;
using DayScribe.Database.Models;

namespace DayScribe.Services;

public class AppStat
{
    public string ProcessName { get; set; } = string.Empty;
    public int DurationSecs { get; set; }
}

public class DomainStat
{
    public string Domain { get; set; } = string.Empty;
    public int DurationSecs { get; set; }
}

public class TodayStats
{
    public List<AppStat> TopApps { get; set; } = [];
    public List<DomainStat> TopDomains { get; set; } = [];
    public int TotalAppTimeSecs { get; set; }
    public int TotalBrowserTimeSecs { get; set; }
}

public class DailyDigestService
{
    private readonly IDbContextFactory<DayScribeDbContext> _contextFactory;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DailyDigestService> _logger;

    public DailyDigestService(
        IDbContextFactory<DayScribeDbContext> contextFactory,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<DailyDigestService> logger)
    {
        _contextFactory = contextFactory;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TodayStats> GetTodayStatsAsync()
    {
        using var db = await _contextFactory.CreateDbContextAsync();
        var todayStart = DateTime.Today.ToUniversalTime();

        // 1. App usage
        var appLogs = await db.ActivityLogs
            .Where(l => l.Timestamp >= todayStart)
            .ToListAsync();

        var topApps = appLogs
            .GroupBy(l => l.ProcessName)
            .Select(g => new AppStat
            {
                ProcessName = g.Key,
                DurationSecs = g.Count() * 2 // 2 seconds per log
            })
            .OrderByDescending(s => s.DurationSecs)
            .ToList();

        var totalAppTime = topApps.Sum(a => a.DurationSecs);

        // 2. Browser events
        var browserEvents = await db.BrowserEvents
            .Where(e => e.Timestamp >= todayStart)
            .ToListAsync();

        var topDomains = browserEvents
            .GroupBy(e => e.Domain)
            .Select(g => new DomainStat
            {
                Domain = string.IsNullOrEmpty(g.Key) ? "Unknown" : g.Key,
                DurationSecs = g.Sum(e => e.DurationSecs)
            })
            .OrderByDescending(s => s.DurationSecs)
            .ToList();

        var totalBrowserTime = topDomains.Sum(d => d.DurationSecs);

        return new TodayStats
        {
            TopApps = topApps,
            TopDomains = topDomains,
            TotalAppTimeSecs = totalAppTime,
            TotalBrowserTimeSecs = totalBrowserTime
        };
    }

    public async Task<string> GenerateDailyDigestAsync()
    {
        _logger.LogInformation("Generating daily digest...");
        var stats = await GetTodayStatsAsync();

        using var db = await _contextFactory.CreateDbContextAsync();
        var today = DateTime.Today;
        var summaries = await db.ArticleSummaries
            .Where(s => s.Date >= today)
            .ToListAsync();

        if (stats.TotalAppTimeSecs == 0 && stats.TotalBrowserTimeSecs == 0 && summaries.Count == 0)
        {
            return "No activity logged for today yet. Keep working and check back later!";
        }

        var appUsageStr = stats.TopApps.Any()
            ? string.Join("\n", stats.TopApps.Take(5).Select(s => $"- {s.ProcessName}: {FormatDuration(s.DurationSecs)}"))
            : "No application activity recorded.";

        var domainUsageStr = stats.TopDomains.Any()
            ? string.Join("\n", stats.TopDomains.Take(5).Select(s => $"- {s.Domain}: {FormatDuration(s.DurationSecs)}"))
            : "No browsing activity recorded.";

        var articlesStr = summaries.Any()
            ? string.Join("\n", summaries.Select(s => $"- [{s.Title}]({s.Url}): {s.Summary}"))
            : "No articles read/summarized today.";

        var prompt = $@"You are 'DayScribe', a helpful daily learning assistant. Provide a daily learning digest summarizing the user's computer activity for today ({today:yyyy-MM-dd}).

Here is their activity data:
---
APPLICATION USAGE (Top 5 apps):
{appUsageStr}

BROWSED DOMAINS (Top 5 domains):
{domainUsageStr}

ARTICLES READ & KEY LEARNINGS:
{articlesStr}
---

Generate a friendly, encouraging, and structured daily learning digest. Break down what they focused on and summarize the main things they learned. Write directly to the user (use 'you'). Be concise but premium in tone.";

        try
        {
            return await CallLlmAsync(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call LLM for daily digest.");
            return $"Failed to generate AI digest. Error: {ex.Message}\n\nHere is your raw activity:\nApps tracked: {stats.TopApps.Count}\nArticles read: {summaries.Count}";
        }
    }

    public async Task<string> AskQuestionAsync(string question)
    {
        _logger.LogInformation("Answering user question: {Question}", question);
        var stats = await GetTodayStatsAsync();

        using var db = await _contextFactory.CreateDbContextAsync();
        var today = DateTime.Today;
        var summaries = await db.ArticleSummaries
            .Where(s => s.Date >= today)
            .ToListAsync();

        var appUsageStr = string.Join("\n", stats.TopApps.Take(10).Select(s => $"- {s.ProcessName}: {FormatDuration(s.DurationSecs)}"));
        var domainUsageStr = string.Join("\n", stats.TopDomains.Take(10).Select(s => $"- {s.Domain}: {FormatDuration(s.DurationSecs)}"));
        var articlesStr = string.Join("\n", summaries.Select(s => $"- {s.Title} ({s.Url}): {s.Summary}"));

        var prompt = $@"You are 'DayScribe', a local-first privacy-preserving desktop assistant. Answer the user's question about their daily computer activity for today ({today:yyyy-MM-dd}) based on the tracked logs below.

Activity data for today:
---
APPLICATION USAGE:
{appUsageStr}

BROWSED DOMAINS:
{domainUsageStr}

ARTICLES SUMMARIZED:
{articlesStr}
---

User's Question: ""{question}""

Answer the question accurately, referencing the data. If the answer cannot be determined from the data, politely explain why.";

        try
        {
            return await CallLlmAsync(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call LLM for Q&A.");
            return $"Error calling AI: {ex.Message}";
        }
    }

    private async Task<string> CallLlmAsync(string prompt)
    {
        var ollamaEndpoint = _configuration.GetValue<string>("AppConfig:Ollama:Endpoint", "http://localhost:11434/api/generate");
        var ollamaModel = _configuration.GetValue<string>("AppConfig:Ollama:Model", "llama3.2:1b");

        try
        {
            using var ollamaCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _httpClient.PostAsJsonAsync(ollamaEndpoint, new
            {
                model = ollamaModel,
                prompt = prompt,
                stream = false
            }, ollamaCts.Token);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ollamaCts.Token);
                if (json.TryGetProperty("response", out var respProp))
                {
                    return respProp.GetString()?.Trim() ?? "No output generated.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Ollama failed: {Message}. Trying OpenAI fallback...", ex.Message);
        }

        // Fallback to OpenAI
        var openAiKey = _configuration.GetValue<string>("AppConfig:OpenAI:ApiKey");
        if (string.IsNullOrWhiteSpace(openAiKey))
        {
            throw new Exception("Ollama failed and OpenAI ApiKey is not configured.");
        }

        var openAiEndpoint = _configuration.GetValue<string>("AppConfig:OpenAI:Endpoint", "https://api.openai.com/v1/chat/completions");
        using var openAiCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
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
        var openAiResponse = await _httpClient.SendAsync(request, openAiCts.Token);

        if (!openAiResponse.IsSuccessStatusCode)
        {
            var errText = await openAiResponse.Content.ReadAsStringAsync(openAiCts.Token);
            throw new Exception($"OpenAI failed: {openAiResponse.StatusCode} - {errText}");
        }

        var aiJson = await openAiResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: openAiCts.Token);
        var content = aiJson
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content?.Trim() ?? "No output generated.";
    }

    private static string FormatDuration(int seconds)
    {
        if (seconds < 60) return $"{seconds}s";
        var minutes = seconds / 60;
        if (minutes < 60) return $"{minutes}m {seconds % 60}s";
        var hours = minutes / 60;
        return $"{hours}h {minutes % 60}m";
    }
}
