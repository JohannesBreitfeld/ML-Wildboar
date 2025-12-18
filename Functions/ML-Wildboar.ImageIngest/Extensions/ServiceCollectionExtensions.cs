using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ML_Wildboar.ImageIngest.Services;
using ML_Wildboar.ImageIngest.Settings;

namespace ML_Wildboar.ImageIngest.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGmailSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GmailSettings>(options =>
        {
            // Key Vault secrets use -- in their names, but KeyVaultSecretManager converts them to : in configuration
            options.ClientId = configuration["Gmail:ClientId"] ?? string.Empty;
            options.ClientSecret = configuration["Gmail:ClientSecret"] ?? string.Empty;
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IImageExtractor, ImageExtractor>();

        return services;
    }
}
