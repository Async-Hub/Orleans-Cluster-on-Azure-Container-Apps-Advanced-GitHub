using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ShoppingApp.Common;
using System.Reflection;

namespace ShoppingApp.SiloHost;

internal static class TelemetryInitializer
{
    internal static void AddApplicationInsights(this IServiceCollection services, string connectionString)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = AppInfo.RetrieveInformationalVersion(assembly);
        const string roleName = "ShoppingApp.SiloHost";
        var instanceId = $"{roleName}-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}-{Guid.NewGuid():N}";

        services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = connectionString;
        });

        services.ConfigureOpenTelemetryTracerProvider((_, tracerBuilder) =>
            tracerBuilder.ConfigureResource(r => r.AddService(
                serviceName: roleName,
                serviceInstanceId: instanceId,
                serviceVersion: version)));

        services.ConfigureOpenTelemetryLoggerProvider((_, loggerBuilder) =>
            loggerBuilder.ConfigureResource(r => r.AddService(
                serviceName: roleName,
                serviceInstanceId: instanceId,
                serviceVersion: version)));
    }
}