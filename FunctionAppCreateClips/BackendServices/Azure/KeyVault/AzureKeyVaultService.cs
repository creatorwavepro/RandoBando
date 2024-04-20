using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;




    public class AzureKeyVaultServices
    {




    public static async Task<string> SetAzureConnectionStringStorageAccountKey()
    {
        var secretName = "AzureConnectionStringStorageAccount";
        string secretValue = Environment.GetEnvironmentVariable(secretName);

        if (string.IsNullOrEmpty(secretValue))
        {
            return "Environment variable not found for " + secretName;
        }

        return secretValue;
    }

    public static async Task<string> SetMainJWTTokenKey()
    {
        var secretName = "SystemSecretKeyJwtEncryption";
        string secretValue = Environment.GetEnvironmentVariable(secretName);

        if (string.IsNullOrEmpty(secretValue))
        {
            return "Environment variable not found for " + secretName;
        }

        return secretValue;
    }

    public static async Task<string> SetExtraJwtTokenEncryption()
    {
      string a = "szdsfs";
        var secretName = "SystemSecretKeySecurelyExtraEncryptJwtTokenHex";
        string secretValue = Environment.GetEnvironmentVariable(secretName);

        if (string.IsNullOrEmpty(secretValue))
        {
            return "Environment variable not found for " + secretName;
        }

        return secretValue;
    }








}

