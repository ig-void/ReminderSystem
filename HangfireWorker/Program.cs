using Backend.Data;
using Backend.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// 1. Connection string (loaded from appsettings.json)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in appsettings.json.");

// 2. EF Core Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Application Services for Email & Notifications
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IReminderNotificationService, ReminderNotificationService>();

// 4. Hangfire Storage & Background Job Server
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(15),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = "DedicatedHangfireWorker";
    options.WorkerCount = Math.Max(Environment.ProcessorCount * 2, 4);
    options.Queues = new[] { "default" };
});

var host = builder.Build();

// 5. Register minutely recurring job to monitor and process due reminders
using (var scope = host.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<IReminderNotificationService>(
        "check-due-reminders",
        service => service.CheckAndSendDueRemindersAsync(),
        Cron.Minutely);
}

Console.WriteLine("==================================================================");
Console.WriteLine("  ReminderApp - Dedicated Hangfire Worker Server Started         ");
Console.WriteLine("  Connected to SQL Server: ReminderAppDb                          ");
Console.WriteLine("  Listening for delayed and queued reminder jobs...               ");
Console.WriteLine("  Press Ctrl+C to shut down gracefully.                          ");
Console.WriteLine("==================================================================");

await host.RunAsync();
