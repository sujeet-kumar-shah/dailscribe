using System.Text.RegularExpressions;
using DayScribe.Web.Models;
using Markdig;

namespace DayScribe.Web.Services;

public class BlogService(IWebHostEnvironment env)
{
    private readonly string _postsDir = Path.Combine(env.ContentRootPath, "Posts");
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public IEnumerable<BlogPost> GetAllPosts()
    {
        if (!Directory.Exists(_postsDir))
            return [];

        return Directory.GetFiles(_postsDir, "*.md")
            .Select(f => ParsePost(Path.GetFileNameWithoutExtension(f), File.ReadAllText(f)))
            .OrderByDescending(p => p.PublishedAt)
            .ToList();
    }

    public BlogPost? GetPost(string slug)
    {
        var path = Path.Combine(_postsDir, $"{slug}.md");
        if (!File.Exists(path)) return null;
        return ParsePost(slug, File.ReadAllText(path));
    }

    private BlogPost ParsePost(string slug, string raw)
    {
        var title = "Untitled";
        var excerpt = "";
        var author = "DayScribe Team";
        var publishedAt = DateTime.UtcNow;
        var tags = new List<string>();
        var coverImage = "";
        var contentStart = 0;

        var lines = raw.Split('\n');
        if (lines.Length > 0 && lines[0].Trim().StartsWith("---"))
        {
            var end = Array.FindIndex(lines, 1, l => l.Trim().StartsWith("---"));
            if (end > 0)
            {
                for (int i = 1; i < end; i++)
                {
                    var line = lines[i];
                    if (line.StartsWith("title:")) title = line["title:".Length..].Trim().Trim('"');
                    else if (line.StartsWith("excerpt:")) excerpt = line["excerpt:".Length..].Trim().Trim('"');
                    else if (line.StartsWith("author:")) author = line["author:".Length..].Trim().Trim('"');
                    else if (line.StartsWith("date:") && DateTime.TryParse(line["date:".Length..].Trim(), out var d)) publishedAt = d;
                    else if (line.StartsWith("tags:")) tags = line["tags:".Length..].Trim().Trim('"').Split(',').Select(t => t.Trim()).ToList();
                    else if (line.StartsWith("cover:")) coverImage = line["cover:".Length..].Trim().Trim('"');
                }
                contentStart = end + 1;
            }
        }

        var body = string.Join("\n", lines.Skip(contentStart)).Trim();
        var html = Markdown.ToHtml(body, _pipeline);

        if (string.IsNullOrEmpty(excerpt))
        {
            var plain = Regex.Replace(body, @"[#*_\[\]()`>|~-]", "");
            excerpt = plain.Length > 200 ? plain[..200] + "..." : plain;
        }

        return new BlogPost
        {
            Slug = slug,
            Title = title,
            Excerpt = excerpt,
            Content = html,
            Author = author,
            PublishedAt = publishedAt,
            Tags = tags,
            CoverImage = coverImage
        };
    }
}
