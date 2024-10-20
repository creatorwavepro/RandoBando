using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;




    public class AzureKeyVaultServices
    {


    public static async Task<string> SetStabilityAIAPIKey()
    {
        // Replace <Your-Key-Vault-URL> with the URL of your Key Vault
        // Replace <Your-Secret-Name> with the name of the secret that holds the storage account connection string
        try
        {


            var secretValue = Environment.GetEnvironmentVariable("StabilityAIpassword");
        

       
            return secretValue;
        }
        catch (Exception ex)
        {

            return "error";
        }
    }

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

    public static async Task<string> SetAzureKeyvaultUrl()
    {
        var secretName = "AzureKeyvaultUrl";
        string secretValue = Environment.GetEnvironmentVariable(secretName);

        if (string.IsNullOrEmpty(secretValue))
        {
            return "Environment variable not found for " + secretName;
        }

        return secretValue;
    }



    public static async Task<string> SetMainJWTTokenKey()
    {
        string s = "sdsdfdss";
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
     
        var secretName = "SystemSecretKeySecurelyExtraEncryptJwtTokenHex";
        string secretValue = Environment.GetEnvironmentVariable(secretName);

        if (string.IsNullOrEmpty(secretValue))
        {
            return "Environment variable not found for " + secretName;
        }

        return secretValue;
    }








}

