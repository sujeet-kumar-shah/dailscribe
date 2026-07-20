using System;

namespace DayScribe.Database.Models;

public class ActivityLogEntry
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public bool IsBrowser { get; set; }

    public void UpdateIsBrowser()
    {
        if (string.IsNullOrEmpty(ProcessName))
        {
            IsBrowser = false;
            return;
        }

        var lower = ProcessName.ToLowerInvariant();
        IsBrowser = lower.Contains("chrome") || 
                    lower.Contains("msedge") || 
                    lower.Contains("firefox") || 
                    lower.Contains("opera") || 
                    lower.Contains("brave");
    }
}
