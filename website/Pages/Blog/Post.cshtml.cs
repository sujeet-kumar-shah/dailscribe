using DayScribe.Web.Models;
using DayScribe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DayScribe.Web.Pages.Blog;

public class BlogPostModel(BlogService blogService) : PageModel
{
    public BlogPost? Post { get; set; }

    public IActionResult OnGet(string slug)
    {
        Post = blogService.GetPost(slug);
        if (Post == null)
            return NotFound();

        return Page();
    }
}
