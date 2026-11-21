using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IReminderNotificationService _notificationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IReminderNotificationService notificationService,
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _notificationService = notificationService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Webhook endpoint to trigger checking and sending due reminder emails immediately.
    /// Can be called by external schedulers, cron jobs, or monitoring triggers.
    /// </summary>
    [HttpPost("check-due-reminders")]
    [HttpPost("trigger")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TriggerDueReminders()
    {
        var secretKey = _configuration["Webhook:SecretKey"];
        if (!string.IsNullOrEmpty(secretKey))
        {
            if (!Request.Headers.TryGetValue("X-Webhook-Secret", out var providedSecret) ||
                providedSecret != secretKey)
            {
                _logger.LogWarning("Unauthorized webhook invocation attempt rejected.");
                return Unauthorized(new { message = "Invalid or missing X-Webhook-Secret header." });
            }
        }

        _logger.LogInformation("Webhook triggered: processing due reminders now...");
        var sentCount = await _notificationService.CheckAndSendDueRemindersAsync();

        return Ok(new
        {
            success = true,
            processedCount = sentCount,
            message = $"Successfully processed {sentCount} due reminder notification(s).",
            timestamp = DateTime.UtcNow
        });
    }
}
