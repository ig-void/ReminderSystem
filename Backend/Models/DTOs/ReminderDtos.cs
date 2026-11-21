using System.ComponentModel.DataAnnotations;

namespace Backend.Models.DTOs;

public class CreateReminderDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Due date is required")]
    public DateTime DueDate { get; set; }

    [EnumDataType(typeof(ReminderPriority), ErrorMessage = "Invalid priority level (1=Low, 2=Medium, 3=High, 4=Urgent)")]
    public ReminderPriority Priority { get; set; } = ReminderPriority.Medium;

    public bool IsPublic { get; set; } = false;
}

public class UpdateReminderDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Due date is required")]
    public DateTime DueDate { get; set; }

    [EnumDataType(typeof(ReminderPriority), ErrorMessage = "Invalid priority level (1=Low, 2=Medium, 3=High, 4=Urgent)")]
    public ReminderPriority Priority { get; set; } = ReminderPriority.Medium;

    public bool IsCompleted { get; set; }

    public bool IsPublic { get; set; }
}

public class ReminderResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public ReminderPriority Priority { get; set; }
    public string PriorityName => Priority.ToString();
    public bool IsCompleted { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string? UserFullName { get; set; }
    public bool IsOwner { get; set; }
}
