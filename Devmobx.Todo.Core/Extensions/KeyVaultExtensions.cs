using System;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Devmobx.Todo.Core.Extensions
{
    public static class KeyVaultExtensions
    {
        public static IConfigurationBuilder ConfigureKeyVault(this IConfigurationBuilder config)
        {
            string keyVaultEndpoint = Environment.GetEnvironmentVariable("KEYVAULT_ENDPOINT");

            if (string.IsNullOrEmpty(keyVaultEndpoint))
            {
                Console.WriteLine("KEYVAULT_ENDPOINT not set - skipping Key Vault configuration");
                return config;
            }

            try
            {
                var secretClient = new SecretClient(
                    new Uri(keyVaultEndpoint),
                    new DefaultAzureCredential()
                );

                config.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                Console.WriteLine($"Key Vault configured: {keyVaultEndpoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Key Vault configuration failed: {ex.Message}");
                throw;
            }

            return config;
        }
    }
}
