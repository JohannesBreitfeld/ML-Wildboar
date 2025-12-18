using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace ML_Wildboar.Shared.Core.Extensions;

/// <summary>
/// Extension methods for configuring application configuration sources.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source if KeyVaultUri is configured.
    /// Uses DefaultAzureCredential which supports Managed Identity, Visual Studio, Azure CLI, etc.
    /// </summary>
    /// <param name="configuration">The configuration builder.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddAzureKeyVaultIfConfigured(this IConfigurationBuilder configuration)
    {
        var config = configuration.Build();
        var keyVaultUri = config["KeyVaultUri"];

        if (!string.IsNullOrEmpty(keyVaultUri))
        {
            var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
            configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
        }

        return configuration;
    }
}
