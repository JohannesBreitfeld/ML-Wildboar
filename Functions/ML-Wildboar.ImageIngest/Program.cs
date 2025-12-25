using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ML_Wildboar.ImageIngest.Extensions;
using ML_Wildboar.Shared.Core.Extensions;
using ML_Wildboar.Shared.Storage.Extensions;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration.AddAzureKeyVault();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddGmailSettings(builder.Configuration)
    .AddQueueSettings(builder.Configuration)
    .AddImageStorage(builder.Configuration)
    .AddApplicationServices();

builder.Build().Run();
  