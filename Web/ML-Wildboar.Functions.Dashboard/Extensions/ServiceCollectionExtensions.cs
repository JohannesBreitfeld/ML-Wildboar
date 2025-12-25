using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ML_Wildboar.Shared.Storage.Repositories;

namespace ML_Wildboar.Functions.Dashboard.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var storageConnectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Storage connection string not configured");

        // Register Azure Storage clients
        services.AddSingleton(new TableServiceClient(storageConnectionString));
        services.AddSingleton(new BlobServiceClient(storageConnectionString));

        // Register ImageRepository
        services.AddScoped<IImageRepository>(sp =>
        {
            var tableClient = sp.GetRequiredService<TableServiceClient>();
            var blobClient = sp.GetRequiredService<BlobServiceClient>();
            var tableName = configuration["AzureStorage:TableName"] ?? "images";
            var containerName = configuration["AzureStorage:BlobContainerName"] ?? "images";

            return new ImageRepository(tableClient, blobClient, tableName, containerName);
        });

        return services;
    }

    public static IServiceCollection AddStaticWebAppCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("SWAPolicy", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:4280",  // SWA CLI local dev
                        "http://localhost:3000",  // React dev server
                        "https://*.azurestaticapps.net"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
