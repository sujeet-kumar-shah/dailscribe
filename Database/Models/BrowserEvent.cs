using System;

namespace DayScribe.Database.Models;

public class BrowserEvent
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DurationSecs { get; set; }
    public string Domain { get; set; } = string.Empty;

    public void UpdateDomain()
    {
        if (string.IsNullOrEmpty(Url))
        {
            Domain = string.Empty;
            return;
        }

        try
        {
            var uri = new Uri(Url);
            Domain = uri.Host;
            if (Domain.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                Domain = Domain.Substring(4);
            }
        }
        catch
        {
            Domain = string.Empty;
        }
    }
}
