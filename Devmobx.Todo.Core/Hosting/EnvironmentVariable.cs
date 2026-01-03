using System;
using System.Collections.Generic;
using System.Text;

namespace Devmobx.Todo.Core.Hosting
{
    public static class EnvironmentVariable
    {
        private static bool _isTest = false;
        public static string KeyVaultEndPoint()
        {
            return _isTest ?
                Environment.GetEnvironmentVariable("KEYVAULT_ENDPOINT", EnvironmentVariableTarget.User) :
                Environment.GetEnvironmentVariable("KEYVAULT_ENDPOINT");
        }

        public static string IdentityId()
        {
            return _isTest ? Environment.GetEnvironmentVariable("SERVICEIDENTITY_CLIENTID", EnvironmentVariableTarget.User) : Environment.GetEnvironmentVariable("SERVICEIDENTITY_CLIENTID");
        }
        public static void SetIsTest(bool isTest)
        {
            _isTest = isTest;
        }


    }


}
