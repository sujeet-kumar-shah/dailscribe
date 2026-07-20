using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DayScribe.Database;
using DayScribe.Database.Models;

namespace DayScribe.Services;

public class ActivityTrackerService : BackgroundService, IActivityTracker
{
    private readonly IDbContextFactory<DayScribeDbContext> _contextFactory;
    private readonly ILogger<ActivityTrackerService> _logger;
    private readonly int _checkIntervalMs = 2000; // 2 seconds
    private readonly int _idleTimeoutMs = 120000; // 120 seconds (2 minutes)

    public bool IsRunning { get; private set; } = true;
    public event Action<string, string>? OnActivityLogged;

    // P/Invoke structures and methods
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    public ActivityTrackerService(
        IDbContextFactory<DayScribeDbContext> contextFactory,
        ILogger<ActivityTrackerService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public void Start()
    {
        IsRunning = true;
        _logger.LogInformation("Activity tracking resumed manually.");
    }

    public void Stop()
    {
        IsRunning = false;
        _logger.LogInformation("Activity tracking paused manually.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ActivityTrackerService starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (IsRunning)
                {
                    await TrackActiveWindowAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during active window tracking.");
            }

            await Task.Delay(_checkIntervalMs, stoppingToken);
        }

        _logger.LogInformation("ActivityTrackerService stopping...");
    }

    private async Task TrackActiveWindowAsync()
    {
        // 1. Check Idle time
        var idleTimeMs = GetIdleTimeMs();
        if (idleTimeMs >= _idleTimeoutMs)
        {
            _logger.LogDebug("User is idle ({IdleSecs}s), skipping logging.", idleTimeMs / 1000);
            return;
        }

        // 2. Get Foreground window info
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        GetWindowThreadProcessId(hWnd, out var processId);
        if (processId == 0)
        {
            return;
        }

        string processName = "Unknown";
        try
        {
            using var process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not resolve process name for PID {Pid}: {Message}", processId, ex.Message);
        }

        var titleBuilder = new StringBuilder(256);
        GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
        var windowTitle = titleBuilder.ToString();

        // 3. Store event in DB
        using var context = await _contextFactory.CreateDbContextAsync();
        var entry = new ActivityLogEntry
        {
            Timestamp = DateTime.UtcNow,
            ProcessName = processName,
            WindowTitle = windowTitle
        };
        entry.UpdateIsBrowser();

        context.ActivityLogs.Add(entry);
        await context.SaveChangesAsync();

        _logger.LogDebug("Logged activity: App={App}, Title={Title}, IsBrowser={IsBrowser}", 
            processName, windowTitle, entry.IsBrowser);

        // Raise notification event
        OnActivityLogged?.Invoke(processName, windowTitle);
    }

    private uint GetIdleTimeMs()
    {
        var lii = new LASTINPUTINFO();
        lii.cbSize = (uint)Marshal.SizeOf(lii);
        if (GetLastInputInfo(ref lii))
        {
            var elapsedTicks = (uint)Environment.TickCount;
            // Handle wrap-around of Environment.TickCount
            return elapsedTicks >= lii.dwTime ? elapsedTicks - lii.dwTime : 0;
        }
        return 0;
    }
}
