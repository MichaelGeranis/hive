using Hive.Application.Interfaces;
using Hive.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Application;

/// <summary>
/// Extension methods for configuring Application layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IDirectReportService, DirectReportService>();
        return services;
    }
}
