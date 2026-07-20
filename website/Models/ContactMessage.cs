using System.ComponentModel.DataAnnotations;

namespace DayScribe.Web.Models;

public class ContactMessage
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required, MaxLength(5000)]
    public string Message { get; set; } = "";

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
