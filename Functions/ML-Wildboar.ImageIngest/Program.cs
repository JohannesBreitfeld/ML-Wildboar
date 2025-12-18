using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ML_Wildboar.ImageIngest.Extensions;
using ML_Wildboar.Shared.Core.Extensions;
using ML_Wildboar.Shared.Storage.Extensions;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration.AddAzureKeyVaultIfConfigured();

// DEBUG: Log configuration values
Console.WriteLine("=== Configuration Debug ===");
Console.WriteLine($"KeyVaultUri: {builder.Configuration["KeyVaultUri"]}");
Console.WriteLine($"AzureStorage--ConnectionString: {(string.IsNullOrEmpty(builder.Configuration["AzureStorage--ConnectionString"]) ? "EMPTY/NULL" : "HAS VALUE")}");
Console.WriteLine($"AzureStorage:ConnectionString: {(string.IsNullOrEmpty(builder.Configuration["AzureStorage:ConnectionString"]) ? "EMPTY/NULL" : "HAS VALUE")}");
Console.WriteLine("========================");

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddGmailSettings(builder.Configuration)
    .AddImageStorage(builder.Configuration) 
    .AddApplicationServices();

builder.Build().Run();
  