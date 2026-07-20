using System;

namespace DayScribe.Database.Models;

public class ArticleSummary
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
