using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ML_Wildboar.Functions.Dashboard.Extensions;
using ML_Wildboar.Shared.Core.Extensions;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration.AddAzureKeyVault();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddAzureStorage(builder.Configuration)
    .AddStaticWebAppCors();

builder.Build().Run();
