namespace DayScribe.Web.Models;

public class BlogPost
{
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Excerpt { get; set; } = "";
    public string Content { get; set; } = "";
    public string Author { get; set; } = "DayScribe Team";
    public DateTime PublishedAt { get; set; }
    public List<string> Tags { get; set; } = [];
    public string CoverImage { get; set; } = "";
}
