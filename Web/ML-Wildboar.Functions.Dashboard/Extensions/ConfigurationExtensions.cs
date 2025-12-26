using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace ML_Wildboar.Functions.Dashboard.Extensions;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddAzureKeyVault(this IConfigurationBuilder configuration)
    {
        var config = configuration.Build();
        var keyVaultUri = config["KeyVaultUri"];

        if (!string.IsNullOrEmpty(keyVaultUri))
        {
            var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
            var keyVaultConfig = new KeyVaultConfigurationSource(secretClient);
            configuration.Add(keyVaultConfig);
        }

        return configuration;
    }
}

internal class KeyVaultConfigurationSource(SecretClient secretClient) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new KeyVaultConfigurationProvider(secretClient);
    }
}

internal class KeyVaultConfigurationProvider(SecretClient secretClient) : ConfigurationProvider
{
    public override void Load()
    {
        var secrets = secretClient.GetPropertiesOfSecrets();
        foreach (var secret in secrets)
        {
            var secretValue = secretClient.GetSecret(secret.Name);
            Data[secret.Name.Replace("--", ":")] = secretValue.Value.Value;
        }
    }
}
