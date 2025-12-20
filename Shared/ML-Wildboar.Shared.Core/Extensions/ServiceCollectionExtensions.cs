using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ML_Wildboar.Shared.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationInsights(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var appInsightsConnectionString = configuration["ApplicationInsights:ConnectionString"];

        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            services.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });
        }

        return services;
    }
}
