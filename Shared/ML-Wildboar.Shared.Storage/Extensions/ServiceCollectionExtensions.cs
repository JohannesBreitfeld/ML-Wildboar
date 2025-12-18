using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ML_Wildboar.Shared.Storage.Repositories;
using ML_Wildboar.Shared.Storage.Settings;

namespace ML_Wildboar.Shared.Storage.Extensions;

/// <summary>
/// Extension methods for registering Azure Storage services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Azure Storage settings, clients, and repositories for image storage.
    /// This is a convenience method that calls all the individual registration methods.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind settings from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddImageStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddAzureStorageSettings(configuration)
            .AddAzureStorageClients()
            .AddImageRepository();
    }

    /// <summary>
    /// Registers Azure Storage settings from configuration.
    /// Settings are read from the "AzureStorage" configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind settings from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureStorageSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureStorageSettings>(options =>
        {
            // Key Vault secrets use -- in their names, but KeyVaultSecretManager converts them to : in configuration
            options.ConnectionString = configuration["AzureStorage:ConnectionString"] ?? string.Empty;
            options.TableName = configuration["AzureStorage:TableName"] ?? "images";
            options.BlobContainerName = configuration["AzureStorage:BlobContainerName"] ?? "images";
        });

        return services;
    }

    /// <summary>
    /// Registers Azure Storage clients (TableServiceClient and BlobServiceClient) as singletons.
    /// Requires AzureStorageSettings to be registered first.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureStorageClients(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<AzureStorageSettings>>().Value;
            return new TableServiceClient(settings.ConnectionString);
        });

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<AzureStorageSettings>>().Value;
            return new BlobServiceClient(settings.ConnectionString);
        });

        return services;
    }

    /// <summary>
    /// Registers the IImageRepository implementation as a scoped service.
    /// Requires Azure Storage clients to be registered first.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddImageRepository(this IServiceCollection services)
    {
        services.AddScoped<IImageRepository>(sp =>
        {
            var tableServiceClient = sp.GetRequiredService<TableServiceClient>();
            var blobServiceClient = sp.GetRequiredService<BlobServiceClient>();
            var settings = sp.GetRequiredService<IOptions<AzureStorageSettings>>().Value;

            return new ImageRepository(
                tableServiceClient,
                blobServiceClient,
                settings.TableName,
                settings.BlobContainerName);
        });

        return services;
    }
}
