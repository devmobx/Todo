using Azure.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Devmobx.Todo.Core.Hosting
{
    public static class IdentityCredential
    {
        public static DefaultAzureCredential Build()
        {
            string identityId = EnvironmentVariable.IdentityId();

            if (string.IsNullOrEmpty(identityId))
                throw new InvalidOperationException("SERVICEIDENTITY_CLIENTID environment variable is missing.");

            return new DefaultAzureCredential(new DefaultAzureCredentialOptions() { ManagedIdentityClientId = identityId });
        }
    }
}
