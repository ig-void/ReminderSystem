using System.Text;
using System.Text.Json;
using Backend.Data;
using Backend.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ReminderNotificationService : IReminderNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReminderNotificationService> _logger;

    public ReminderNotificationService(
        ApplicationDbContext context,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobs,
        IHttpClientFactory httpClientFactory,
        ILogger<ReminderNotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _backgroundJobs = backgroundJobs;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string? ScheduleReminderJob(int reminderId, DateTime dueDate, int remindBeforeMinutes)
    {
        var dueUtc = dueDate.Kind == DateTimeKind.Utc ? dueDate : dueDate.ToUniversalTime();
        var notifyAtUtc = dueUtc.AddMinutes(-remindBeforeMinutes);
        var delay = notifyAtUtc - DateTime.UtcNow;

        string jobId;
        if (delay > TimeSpan.Zero)
        {
            jobId = _backgroundJobs.Schedule<IReminderNotificationService>(
                service => service.SendReminderNotificationAsync(reminderId),
                delay);

            _logger.LogInformation(
                "Scheduled Hangfire notification job {JobId} for reminder {ReminderId} in {DelayMinutes:F1} minutes (at {NotifyAtUtc} UTC).",
                jobId, reminderId, delay.TotalMinutes, notifyAtUtc);
        }
        else
        {
            // Immediate dispatch if notify time has already arrived
            jobId = _backgroundJobs.Enqueue<IReminderNotificationService>(
                service => service.SendReminderNotificationAsync(reminderId));

            _logger.LogInformation(
                "Enqueued immediate Hangfire notification job {JobId} for reminder {ReminderId}.",
                jobId, reminderId);
        }

        return jobId;
    }

    public void CancelReminderJob(string? jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return;
        }

        try
        {
            _backgroundJobs.Delete(jobId);
            _logger.LogInformation("Deleted Hangfire job {JobId}.", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete Hangfire job {JobId}.", jobId);
        }
    }

    public async Task SendReminderNotificationAsync(int reminderId)
    {
        var reminder = await _context.Reminders
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reminderId);

        if (reminder == null)
        {
            _logger.LogWarning("Reminder {ReminderId} not found for notification.", reminderId);
            return;
        }

        if (reminder.IsCompleted)
        {
            _logger.LogInformation("Reminder {ReminderId} is already completed. Skipping notification.", reminderId);
            return;
        }

        if (reminder.IsEmailSent)
        {
            _logger.LogInformation("Notification already sent for reminder {ReminderId}. Skipping.", reminderId);
            return;
        }

        if (reminder.User == null)
        {
            _logger.LogWarning("User not found for reminder {ReminderId}.", reminderId);
            return;
        }

        // 1. Send Email Notification
        try
        {
            await _emailService.SendReminderEmailAsync(reminder, reminder.User);
            reminder.IsEmailSent = true;
            reminder.EmailSentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Email notification sent for reminder {ReminderId}.", reminderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification for reminder {ReminderId}.", reminderId);
            throw; // Let Hangfire retry if transient failure
        }

        // 2. Dispatch Outgoing Webhook if configured
        if (!string.IsNullOrWhiteSpace(reminder.WebhookUrl))
        {
            await DispatchWebhookAsync(reminder);
        }
    }

    public async Task<int> CheckAndSendDueRemindersAsync()
    {
        var nowUtc = DateTime.UtcNow;

        var dueReminders = await _context.Reminders
            .Include(r => r.User)
            .Where(r => !r.IsCompleted && !r.IsEmailSent)
            .ToListAsync();

        var dueList = dueReminders
            .Where(r =>
            {
                var dueUtc = r.DueDate.Kind == DateTimeKind.Utc ? r.DueDate : r.DueDate.ToUniversalTime();
                return nowUtc >= dueUtc.AddMinutes(-r.RemindBeforeMinutes);
            })
            .ToList();

        int sentCount = 0;
        foreach (var reminder in dueList)
        {
            try
            {
                if (reminder.User != null)
                {
                    await _emailService.SendReminderEmailAsync(reminder, reminder.User);
                    reminder.IsEmailSent = true;
                    reminder.EmailSentAt = DateTime.UtcNow;

                    if (!string.IsNullOrWhiteSpace(reminder.WebhookUrl))
                    {
                        await DispatchWebhookAsync(reminder);
                    }

                    sentCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring due reminder {ReminderId}.", reminder.Id);
            }
        }

        if (sentCount > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Processed and dispatched {Count} due reminder notification(s).", sentCount);
        }

        return sentCount;
    }

    private async Task DispatchWebhookAsync(Reminder reminder)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                @event = "reminder.notification",
                reminderId = reminder.Id,
                title = reminder.Title,
                description = reminder.Description,
                dueDate = reminder.DueDate,
                priority = reminder.Priority.ToString(),
                remindBeforeMinutes = reminder.RemindBeforeMinutes,
                userEmail = reminder.User?.Email,
                triggeredAtUtc = DateTime.UtcNow
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(reminder.WebhookUrl, content);
            _logger.LogInformation(
                "Webhook delivered for reminder {ReminderId} to {Url}. Status: {StatusCode}",
                reminder.Id, reminder.WebhookUrl, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deliver webhook for reminder {ReminderId} to {Url}", reminder.Id, reminder.WebhookUrl);
        }
    }
}
