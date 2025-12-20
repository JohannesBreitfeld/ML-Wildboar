using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace ML_Wildboar.Shared.Core.Extensions;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddAzureKeyVault(this IConfigurationBuilder configuration)
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
