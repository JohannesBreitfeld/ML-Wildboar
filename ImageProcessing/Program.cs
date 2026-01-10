using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ML;
using ML_Wildboar.ImageProcessor;
using ML_Wildboar.ImageProcessor.Services;
using ML_Wildboar.ImageProcessor.Settings;
using ML_Wildboar.Shared.Core.Extensions;
using ML_Wildboar.Shared.Storage.Extensions;
using WildboarMonitor_FunctionApp;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Configuration.AddAzureKeyVault();

builder.Services.AddApplicationInsights(builder.Configuration);
builder.Logging.AddApplicationInsights();

builder.Services.Configure<ProcessingSettings>(
    builder.Configuration.GetSection("Processing"));

builder.Services.AddImageStorage(builder.Configuration);
builder.Services.AddQueueSettings(builder.Configuration);

var modelPath = Path.Combine(AppContext.BaseDirectory, "WildboarModel.mlnet");
if (!File.Exists(modelPath))
{
    throw new FileNotFoundException($"ML model not found at: {modelPath}");
}

builder.Services.AddPredictionEnginePool<MLModel.ModelInput, MLModel.ModelOutput>()
    .FromFile(modelPath);

builder.Services.AddSingleton<IImageProcessingService, ImageProcessingService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
