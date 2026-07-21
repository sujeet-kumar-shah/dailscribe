using DayScribe.Web.Models;
using DayScribe.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DayScribe.Web.Pages.Blog;

public class BlogIndexModel(BlogService blogService) : PageModel
{
    public IEnumerable<BlogPost> Posts { get; set; } = [];

    public void OnGet()
    {
        Posts = blogService.GetAllPosts();
    }
}
