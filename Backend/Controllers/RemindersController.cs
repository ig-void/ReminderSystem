using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Models.DTOs;
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
    private readonly ILogger<RemindersController> _logger;

    public RemindersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<RemindersController> logger)
    {
        _context = context;
        _userManager = userManager;
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

        // Filter: Search term
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(r => r.Title.ToLower().Contains(searchLower) ||
                                     (r.Description != null && r.Description.ToLower().Contains(searchLower)));
        }

        // Filter: Completion status
        if (isCompleted.HasValue)
        {
            query = query.Where(r => r.IsCompleted == isCompleted.Value);
        }

        // Filter: Priority
        if (priority.HasValue)
        {
            query = query.Where(r => r.Priority == priority.Value);
        }

        // Sorting
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

        // Filter: Search term
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(r => r.Title.ToLower().Contains(searchLower) ||
                                     (r.Description != null && r.Description.ToLower().Contains(searchLower)));
        }

        // Filter: Completion status
        if (isCompleted.HasValue)
        {
            query = query.Where(r => r.IsCompleted == isCompleted.Value);
        }

        // Filter: Priority
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

        // Allow view if owned by current user or marked as public
        if (reminder.UserId != currentUserId && !reminder.IsPublic)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to view this reminder." });
        }

        return Ok(MapToDto(reminder, currentUserId));
    }

    /// <summary>
    /// Add a new reminder
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

        var reminder = new Reminder
        {
            Title = createDto.Title.Trim(),
            Description = createDto.Description?.Trim(),
            DueDate = createDto.DueDate,
            Priority = createDto.Priority,
            IsPublic = createDto.IsPublic,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UserId = currentUserId,
            User = currentUser
        };

        _context.Reminders.Add(reminder);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Reminder created with ID {Id} for user {UserId}", reminder.Id, currentUserId);

        var responseDto = MapToDto(reminder, currentUserId);
        return CreatedAtAction(nameof(GetById), new { id = reminder.Id }, responseDto);
    }

    /// <summary>
    /// Edit / Update an existing reminder
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

        reminder.Title = updateDto.Title.Trim();
        reminder.Description = updateDto.Description?.Trim();
        reminder.DueDate = updateDto.DueDate;
        reminder.Priority = updateDto.Priority;
        reminder.IsCompleted = updateDto.IsCompleted;
        reminder.IsPublic = updateDto.IsPublic;
        reminder.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reminder with ID {Id} updated by user {UserId}", id, currentUserId);

        return Ok(MapToDto(reminder, currentUserId));
    }

    /// <summary>
    /// Delete a reminder
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
            DueDate = reminder.DueDate,
            Priority = reminder.Priority,
            IsCompleted = reminder.IsCompleted,
            IsPublic = reminder.IsPublic,
            CreatedAt = reminder.CreatedAt,
            UpdatedAt = reminder.UpdatedAt,
            UserId = reminder.UserId,
            UserEmail = reminder.User?.Email,
            UserFullName = string.IsNullOrEmpty(fullName) ? reminder.User?.UserName : fullName,
            IsOwner = reminder.UserId == currentUserId
        };
    }
}
