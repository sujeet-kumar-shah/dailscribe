using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DayScribe.Web.Services;

public class EmailSender(IConfiguration config, ILogger<EmailSender> logger)
{
    public async Task<bool> SendContactEmailAsync(string name, string email, string message)
    {
        try
        {
            var smtpSection = config.GetSection("Smtp");
            var host = smtpSection["Host"];
            if (string.IsNullOrEmpty(host))
            {
                logger.LogInformation("SMTP not configured; contact message from {Name} <{Email}>: {Message}", name, email, message);
                return true;
            }

            var fromName = smtpSection["FromName"] ?? "DayScribe";
            var fromAddr = smtpSection["FromAddress"] ?? "noreply@dayscribe.app";
            var toAddr = smtpSection["ToAddress"] ?? "hello@dayscribe.app";
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(fromName, fromAddr));
            msg.To.Add(new MailboxAddress("DayScribe Support", toAddr));
            msg.Subject = $"New contact from {name}";

            var body = new BodyBuilder
            {
                TextBody = $"Name: {name}\nEmail: {email}\n\n{message}",
                HtmlBody = $"<p><strong>Name:</strong> {name}</p><p><strong>Email:</strong> {email}</p><hr/><p>{message}</p>"
            };
            msg.Body = body.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(host, int.Parse(smtpSection["Port"] ?? "587"), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpSection["Username"] ?? "", smtpSection["Password"] ?? "");
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);

            logger.LogInformation("Contact email sent from {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send contact email from {Email}", email);
            return false;
        }
    }
}
