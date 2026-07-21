using DayScribe.Web.Models;
using DayScribe.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DayScribe.Web.Pages;

public class ContactModel(EmailSender emailSender) : PageModel
{
    [BindProperty]
    public ContactMessage Contact { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var sent = await emailSender.SendContactEmailAsync(Contact.Name, Contact.Email, Contact.Message);
        SuccessMessage = sent
            ? "Thank you! Your message has been sent. We'll get back to you soon."
            : "Message received (email delivery is not configured). We'll review it shortly.";

        ModelState.Clear();
        Contact = new();
        return Page();
    }
}
