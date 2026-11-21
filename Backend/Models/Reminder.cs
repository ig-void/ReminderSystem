using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public enum ReminderPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public class Reminder
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime DueDate { get; set; }

    public ReminderPriority Priority { get; set; } = ReminderPriority.Medium;

    public bool IsCompleted { get; set; } = false;

    public bool IsPublic { get; set; } = false;

    // Notification configuration: Lead time before due date in minutes (e.g. 5, 15, 30, 60, etc.)
    public int RemindBeforeMinutes { get; set; } = 15;

    public bool IsEmailSent { get; set; } = false;

    public DateTime? EmailSentAt { get; set; }

    [MaxLength(100)]
    public string? HangfireJobId { get; set; }

    [MaxLength(500)]
    public string? WebhookUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }
}
