using System.Net;
using System.Net.Mail;
using Backend.Models;

namespace Backend.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var smtpHost = _configuration["Smtp:Host"];
        var smtpPortString = _configuration["Smtp:Port"];
        var smtpUser = _configuration["Smtp:UserName"];
        var smtpPass = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:FromEmail"] ?? "no-reply@reminderapp.local";
        var fromName = _configuration["Smtp:FromName"] ?? "Reminder Application";

        // If no SMTP host is configured, log the email to console/logs (ideal for development/testing)
        if (string.IsNullOrWhiteSpace(smtpHost) || smtpHost == "localhost" && string.IsNullOrWhiteSpace(smtpUser))
        {
            _logger.LogInformation(
                "\n================ [EMAIL NOTIFICATION DISPATCHED] ================\n" +
                "To: {ToEmail}\nSubject: {Subject}\nTimestamp: {Timestamp}\nContent:\n{Body}\n" +
                "=================================================================",
                toEmail, subject, DateTime.UtcNow, htmlBody);
            return;
        }

        int smtpPort = int.TryParse(smtpPortString, out var port) ? port : 587;
        bool enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) ? ssl : true;

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = enableSsl,
            Credentials = !string.IsNullOrEmpty(smtpUser)
                ? new NetworkCredential(smtpUser, smtpPass)
                : CredentialCache.DefaultNetworkCredentials
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Email successfully sent to {ToEmail} with subject '{Subject}'.", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}. Fallback logged.", toEmail);
            throw;
        }
    }

    public async Task SendReminderEmailAsync(Reminder reminder, ApplicationUser user)
    {
        var recipientEmail = user.Email;
        if (string.IsNullOrEmpty(recipientEmail))
        {
            _logger.LogWarning("Cannot send reminder email: User {UserId} has no email address.", user.Id);
            return;
        }

        var userName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.UserName ?? "User";
        var subject = $"Reminder: {reminder.Title} is due soon";

        var html = $@"
<!DOCTYPE html>
<html>
<head>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; color: #1e293b; padding: 24px; margin: 0; }}
    .card {{ background-color: #ffffff; max-width: 580px; margin: 0 auto; border-radius: 12px; border: 1px solid #e2e8f0; padding: 32px; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }}
    .badge {{ display: inline-block; padding: 4px 10px; font-size: 12px; font-weight: 600; border-radius: 6px; }}
    .badge-urgent {{ background-color: #ffe4e6; color: #e11d48; }}
    .badge-high {{ background-color: #fef3c7; color: #d97706; }}
    .badge-medium {{ background-color: #e0f2fe; color: #0284c7; }}
    .badge-low {{ background-color: #f1f5f9; color: #475569; }}
    .due-box {{ background-color: #f8fafc; border-left: 4px solid #0f172a; padding: 12px 16px; margin: 20px 0; border-radius: 0 8px 8px 0; }}
    .footer {{ margin-top: 32px; font-size: 12px; color: #64748b; text-align: center; border-top: 1px solid #f1f5f9; padding-top: 16px; }}
  </style>
</head>
<body>
  <div class='card'>
    <h2 style='margin-top: 0; color: #0f172a;'>Reminder Notification</h2>
    <p>Hello {WebUtility.HtmlEncode(userName)},</p>
    <p>This is an automated notification for your upcoming reminder scheduled to be due in <strong>{reminder.RemindBeforeMinutes} minutes</strong>.</p>
    
    <div class='due-box'>
      <h3 style='margin: 0 0 6px 0; color: #0f172a;'>{WebUtility.HtmlEncode(reminder.Title)}</h3>
      {(string.IsNullOrWhiteSpace(reminder.Description) ? "" : $"<p style='margin: 0 0 8px 0; color: #475569;'>{WebUtility.HtmlEncode(reminder.Description)}</p>")}
      <p style='margin: 0; font-size: 13px;'><strong>Due Date:</strong> {reminder.DueDate:yyyy-MM-dd HH:mm UTC}</p>
      <div style='margin-top: 8px;'>
        <span class='badge badge-{reminder.Priority.ToString().ToLower()}'>Priority: {reminder.Priority}</span>
      </div>
    </div>

    <p style='font-size: 13px; color: #64748b;'>You configured this alert to arrive {reminder.RemindBeforeMinutes} minute(s) before the scheduled due time.</p>
    <div class='footer'>
      Reminder Application Workspace &bull; Automated notification
    </div>
  </div>
</body>
</html>";

        await SendEmailAsync(recipientEmail, subject, html);
    }
}
