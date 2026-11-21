using Backend.Models;

namespace Backend.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    Task SendReminderEmailAsync(Reminder reminder, ApplicationUser user);
}
