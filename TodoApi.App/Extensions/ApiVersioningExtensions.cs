using Microsoft.AspNetCore.Mvc;

namespace TodoApi.App.Extensions;

public static class ApiVersioningExtensions
{
    public static IServiceCollection ConfigureApiVersion(this IServiceCollection services, int majorVersion, int minorVersion)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(majorVersion, minorVersion);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}

