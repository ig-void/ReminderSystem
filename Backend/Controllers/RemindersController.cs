using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Models.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RemindersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IReminderNotificationService _notificationService;
    private readonly ILogger<RemindersController> _logger;

    public RemindersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IReminderNotificationService notificationService,
        ILogger<RemindersController> logger)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
        _logger = logger;
    }

    private string? GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// View own reminders with optional filtering and search
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReminderResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReminders(
        [FromQuery] string? search = null,
        [FromQuery] bool? isCompleted = null,
        [FromQuery] ReminderPriority? priority = null,
        [FromQuery] string? sortBy = "duedate",
        [FromQuery] bool isDescending = false)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var query = _context.Reminders
            .Include(r => r.User)
            .Where(r => r.UserId == currentUserId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(r => r.Title.ToLower().Contains(searchLower) ||
                                     (r.Description != null && r.Description.ToLower().Contains(searchLower)));
        }

        if (isCompleted.HasValue)
        {
            query = query.Where(r => r.IsCompleted == isCompleted.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(r => r.Priority == priority.Value);
        }

        query = (sortBy?.ToLower()) switch
        {
            "priority" => isDescending ? query.OrderByDescending(r => r.Priority) : query.OrderBy(r => r.Priority),
            "createdat" => isDescending ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
            _ => isDescending ? query.OrderByDescending(r => r.DueDate) : query.OrderBy(r => r.DueDate)
        };

        var reminders = await query.ToListAsync();
        var result = reminders.Select(r => MapToDto(r, currentUserId));

        return Ok(result);
    }

    /// <summary>
    /// View public/shared reminders created by other users
    /// </summary>
    [HttpGet("others")]
    [ProducesResponseType(typeof(IEnumerable<ReminderResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOthersReminders(
        [FromQuery] string? search = null,
        [FromQuery] ReminderPriority? priority = null,
        [FromQuery] bool? isCompleted = null)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var query = _context.Reminders
            .Include(r => r.User)
            .Where(r => r.UserId != currentUserId && r.IsPublic == true);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(r => r.Title.ToLower().Contains(searchLower) ||
                                     (r.Description != null && r.Description.ToLower().Contains(searchLower)));
        }

        if (isCompleted.HasValue)
        {
            query = query.Where(r => r.IsCompleted == isCompleted.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(r => r.Priority == priority.Value);
        }

        var reminders = await query
            .OrderBy(r => r.DueDate)
            .ToListAsync();

        var result = reminders.Select(r => MapToDto(r, currentUserId));

        return Ok(result);
    }

    /// <summary>
    /// View a specific reminder by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReminderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var reminder = await _context.Reminders
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reminder == null)
        {
            return NotFound(new { message = $"Reminder with ID {id} was not found." });
        }

        if (reminder.UserId != currentUserId && !reminder.IsPublic)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to view this reminder." });
        }

        return Ok(MapToDto(reminder, currentUserId));
    }

    /// <summary>
    /// Add a new reminder and schedule Hangfire email notification
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReminderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateReminderDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var currentUser = await _userManager.FindByIdAsync(currentUserId);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var dueDateUtc = createDto.DueDate.Kind switch
        {
            DateTimeKind.Utc => createDto.DueDate,
            DateTimeKind.Local => createDto.DueDate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(createDto.DueDate, DateTimeKind.Utc)
        };

        var reminder = new Reminder
        {
            Title = createDto.Title.Trim(),
            Description = createDto.Description?.Trim(),
            DueDate = dueDateUtc,
            Priority = createDto.Priority,
            IsPublic = createDto.IsPublic,
            RemindBeforeMinutes = createDto.RemindBeforeMinutes,
            WebhookUrl = createDto.WebhookUrl?.Trim(),
            IsCompleted = false,
            IsEmailSent = false,
            CreatedAt = DateTime.UtcNow,
            UserId = currentUserId,
            User = currentUser
        };

        _context.Reminders.Add(reminder);
        await _context.SaveChangesAsync();

        // Schedule Hangfire background notification job
        var jobId = _notificationService.ScheduleReminderJob(
            reminder.Id,
            reminder.DueDate,
            reminder.RemindBeforeMinutes);

        if (!string.IsNullOrEmpty(jobId))
        {
            reminder.HangfireJobId = jobId;
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Reminder created with ID {Id} for user {UserId}. Hangfire Job: {JobId}", reminder.Id, currentUserId, jobId);

        var responseDto = MapToDto(reminder, currentUserId);
        return CreatedAtAction(nameof(GetById), new { id = reminder.Id }, responseDto);
    }

    /// <summary>
    /// Edit / Update an existing reminder and reschedule Hangfire notification
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ReminderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReminderDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var reminder = await _context.Reminders
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reminder == null)
        {
            return NotFound(new { message = $"Reminder with ID {id} was not found." });
        }

        if (reminder.UserId != currentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only edit your own reminders." });
        }

        var updateDueUtc = updateDto.DueDate.Kind switch
        {
            DateTimeKind.Utc => updateDto.DueDate,
            DateTimeKind.Local => updateDto.DueDate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(updateDto.DueDate, DateTimeKind.Utc)
        };
        bool notificationConfigChanged = reminder.DueDate != updateDueUtc ||
                                        reminder.RemindBeforeMinutes != updateDto.RemindBeforeMinutes;

        reminder.Title = updateDto.Title.Trim();
        reminder.Description = updateDto.Description?.Trim();
        reminder.DueDate = updateDueUtc;
        reminder.Priority = updateDto.Priority;
        reminder.IsCompleted = updateDto.IsCompleted;
        reminder.IsPublic = updateDto.IsPublic;
        reminder.RemindBeforeMinutes = updateDto.RemindBeforeMinutes;
        reminder.WebhookUrl = updateDto.WebhookUrl?.Trim();
        reminder.UpdatedAt = DateTime.UtcNow;

        if (reminder.IsCompleted)
        {
            // Cancel notification if completed
            _notificationService.CancelReminderJob(reminder.HangfireJobId);
            reminder.HangfireJobId = null;
        }
        else if (notificationConfigChanged && !reminder.IsEmailSent)
        {
            // Reschedule notification
            _notificationService.CancelReminderJob(reminder.HangfireJobId);
            var newJobId = _notificationService.ScheduleReminderJob(
                reminder.Id,
                reminder.DueDate,
                reminder.RemindBeforeMinutes);

            reminder.HangfireJobId = newJobId;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reminder with ID {Id} updated by user {UserId}", id, currentUserId);

        return Ok(MapToDto(reminder, currentUserId));
    }

    /// <summary>
    /// Delete a reminder and cancel any scheduled Hangfire notification
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var reminder = await _context.Reminders.FirstOrDefaultAsync(r => r.Id == id);
        if (reminder == null)
        {
            return NotFound(new { message = $"Reminder with ID {id} was not found." });
        }

        if (reminder.UserId != currentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete your own reminders." });
        }

        _notificationService.CancelReminderJob(reminder.HangfireJobId);

        _context.Reminders.Remove(reminder);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Reminder with ID {Id} deleted by user {UserId}", id, currentUserId);

        return Ok(new { message = "Reminder deleted successfully.", id });
    }

    /// <summary>
    /// Toggle completion status of a reminder
    /// </summary>
    [HttpPatch("{id:int}/toggle-complete")]
    [ProducesResponseType(typeof(ReminderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ToggleComplete(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var reminder = await _context.Reminders
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reminder == null)
        {
            return NotFound(new { message = $"Reminder with ID {id} was not found." });
        }

        if (reminder.UserId != currentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only update your own reminders." });
        }

        reminder.IsCompleted = !reminder.IsCompleted;
        reminder.UpdatedAt = DateTime.UtcNow;

        if (reminder.IsCompleted)
        {
            _notificationService.CancelReminderJob(reminder.HangfireJobId);
            reminder.HangfireJobId = null;
        }
        else if (!reminder.IsEmailSent)
        {
            var jobId = _notificationService.ScheduleReminderJob(
                reminder.Id,
                reminder.DueDate,
                reminder.RemindBeforeMinutes);

            reminder.HangfireJobId = jobId;
        }

        await _context.SaveChangesAsync();

        return Ok(MapToDto(reminder, currentUserId));
    }

    private static ReminderResponseDto MapToDto(Reminder reminder, string currentUserId)
    {
        var fullName = string.Join(" ", new[] { reminder.User?.FirstName, reminder.User?.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

        return new ReminderResponseDto
        {
            Id = reminder.Id,
            Title = reminder.Title,
            Description = reminder.Description,
            DueDate = EnsureUtc(reminder.DueDate),
            Priority = reminder.Priority,
            IsCompleted = reminder.IsCompleted,
            IsPublic = reminder.IsPublic,
            RemindBeforeMinutes = reminder.RemindBeforeMinutes,
            IsEmailSent = reminder.IsEmailSent,
            EmailSentAt = EnsureUtc(reminder.EmailSentAt),
            WebhookUrl = reminder.WebhookUrl,
            CreatedAt = EnsureUtc(reminder.CreatedAt),
            UpdatedAt = EnsureUtc(reminder.UpdatedAt),
            UserId = reminder.UserId,
            UserEmail = reminder.User?.Email,
            UserFullName = string.IsNullOrEmpty(fullName) ? reminder.User?.UserName : fullName,
            IsOwner = reminder.UserId == currentUserId
        };
    }

    private static DateTime EnsureUtc(DateTime dt)
    {
        return dt.Kind == DateTimeKind.Utc ? dt : (dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() : DateTime.SpecifyKind(dt, DateTimeKind.Utc));
    }

    private static DateTime? EnsureUtc(DateTime? dt)
    {
        if (!dt.HasValue) return null;
        return dt.Value.Kind == DateTimeKind.Utc ? dt.Value : (dt.Value.Kind == DateTimeKind.Local ? dt.Value.ToUniversalTime() : DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc));
    }
}
