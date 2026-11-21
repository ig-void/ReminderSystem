using Hangfire.Dashboard;

namespace Backend.Common;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow all local / development requests
        var env = httpContext.RequestServices.GetService<IHostEnvironment>();
        if (env != null && env.IsDevelopment())
        {
            return true;
        }

        // Allow authenticated users in production
        return httpContext.User.Identity?.IsAuthenticated ?? false;
    }
}
