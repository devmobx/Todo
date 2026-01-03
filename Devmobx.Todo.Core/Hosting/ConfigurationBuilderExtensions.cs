using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using System;

namespace Devmobx.Todo.Core.Hosting
{
    public static class ConfigurationBuilderExtensions
    {
        public static void ConfigureKeyVault(this IConfigurationBuilder config)
        {
            string keyVaultEndpoint = EnvironmentVariable.KeyVaultEndPoint();


            if (string.IsNullOrEmpty(keyVaultEndpoint))
                throw new InvalidOperationException("KEYVAULT_ENDPOINT environment variable is missing.");

            var secretClient = new SecretClient(new Uri(keyVaultEndpoint), IdentityCredential.Build());
            config.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());

        }
    }
}
