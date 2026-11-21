namespace Backend.Services;

public interface IReminderNotificationService
{
    string? ScheduleReminderJob(int reminderId, DateTime dueDate, int remindBeforeMinutes);
    void CancelReminderJob(string? jobId);
    Task SendReminderNotificationAsync(int reminderId);
    Task<int> CheckAndSendDueRemindersAsync();
}
