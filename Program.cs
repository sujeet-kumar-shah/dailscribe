using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DayScribe.Database;
using DayScribe.Database.Models;
using DayScribe.Services;

namespace DayScribe;

public class Program
{
    private static WebApplication? _webApp;
    public static WebApplication? WebApp => _webApp;

    [STAThread]
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Configuration
        builder.Configuration.SetBasePath(AppContext.BaseDirectory);
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        // Add services to the container
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Register EF Core DbContextFactory
        builder.Services.AddDbContextFactory<DayScribeDbContext>();

        // Register Activity Tracker Services
        builder.Services.AddSingleton<IActivityTracker, ActivityTrackerService>();
        builder.Services.AddHostedService(sp => (ActivityTrackerService)sp.GetRequiredService<IActivityTracker>());

        // Register AI and other business logic services
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<ArticleSummarizerService>();
        builder.Services.AddSingleton<DailyDigestService>();

        // CORS to allow browser extension requests
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        // Get Port from configuration, default to 9103 -- set BEFORE Build()
        var port = builder.Configuration.GetValue<int>("AppConfig:LocalApiPort", 9103);
        // Configure Kestrel URL by setting it through the configuration system
        builder.Configuration["urls"] = $"http://localhost:{port}";

        _webApp = builder.Build();

        // Ensure database is migrated/created on startup
        using (var scope = _webApp.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DayScribeDbContext>();
            db.Database.Migrate();
        }

        // Configure pipeline
        if (!_webApp.Environment.IsDevelopment())
        {
            _webApp.UseExceptionHandler("/Error", createScopeForErrors: true);
        }

        _webApp.UseStaticFiles();
        _webApp.UseAntiforgery();
        _webApp.UseCors();

        // Web API Endpoint for Chrome Extension
        _webApp.MapPost("/api/browser/activity", async (BrowserActivityDto dto, IDbContextFactory<DayScribeDbContext> dbFactory, ILogger<Program> logger) =>
        {
            logger.LogInformation("Received browser activity: {Url} for {Duration}s", dto.Url, dto.TimeSpentSecs);
            
            if (string.IsNullOrEmpty(dto.Url))
            {
                return Results.BadRequest("URL is required.");
            }

            using var db = await dbFactory.CreateDbContextAsync();
            var browserEvent = new BrowserEvent
            {
                Timestamp = dto.Timestamp ?? DateTime.UtcNow,
                Url = dto.Url,
                Title = dto.Title ?? string.Empty,
                DurationSecs = dto.TimeSpentSecs
            };
            browserEvent.UpdateDomain();
            db.BrowserEvents.Add(browserEvent);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        _webApp.MapRazorComponents<Components.App>()
            .AddInteractiveServerRenderMode();

        // Start Kestrel on background thread (URL already set via UseUrls above)
        Task.Run(() => _webApp.RunAsync());

        // Initialize and Run WPF App on main UI thread
        var wpfApp = new App();
        wpfApp.Run();

        // Shutdown Kestrel cleanly when WPF App closes
        _webApp.StopAsync().GetAwaiter().GetResult();
    }
}

public record BrowserActivityDto(string Url, string Title, int TimeSpentSecs, DateTime? Timestamp);
