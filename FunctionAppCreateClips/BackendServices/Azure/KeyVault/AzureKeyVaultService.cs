using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;




    public class AzureKeyVaultServices
    {

  


    // Make sure the method is async and returns a Task<string>
    public static async Task<string> SetOpenAIAPIKey()
    {
        // Replace <Your-Key-Vault-URL> with the URL of your Key Vault
        // Replace <Your-Secret-Name> with the name of the secret that holds the storage account connection string
        try
        {

      
        var keyVaultUrl = new Uri("https://kevault-west-europe.vault.azure.net/");
        var secretName = "ApiOpenAIsecretKey";

        // Build a secret client using the DefaultAzureCredential which uses the managed identity or service principal
        var client = new SecretClient(keyVaultUrl, new DefaultAzureCredential());

        // Retrieve a secret
        KeyVaultSecret secret = await client.GetSecretAsync(secretName);
        string secretValue = secret.Value;

        // Return the secret value
        return secretValue;
        }
        catch (Exception ex)
        {

            return "an error occured receiving secrets => " + ex.Message;
        }
    }

    public static async Task<string> SetStabilityAIAPIKey()
    {
        // Replace <Your-Key-Vault-URL> with the URL of your Key Vault
        // Replace <Your-Secret-Name> with the name of the secret that holds the storage account connection string
        try
        {
      
        var keyVaultUrl = new Uri("https://kevault-west-europe.vault.azure.net/");
        var secretName = "ApiStabilityAIKey";

        // Build a secret client using the DefaultAzureCredential which uses the managed identity or service principal
        var client = new SecretClient(keyVaultUrl, new DefaultAzureCredential());

        // Retrieve a secret
        KeyVaultSecret secret = await client.GetSecretAsync(secretName);
        string secretValue = secret.Value;

        // Return the secret value
        return secretValue;
        }
        catch (Exception ex)
        {

            return "an error occured receiving secrets => " + ex.Message;
        }
    }

    public static async Task<string> SetAzureConnectionStringStorageAccountKey()
    {
        // Replace <Your-Key-Vault-URL> with the URL of your Key Vault
        // Replace <Your-Secret-Name> with the name of the secret that holds the storage account connection string
        try
        {


        var keyVaultUrl = new Uri("https://kevault-west-europe.vault.azure.net/");
        var secretName = "AzureConnectinoStringStorageAccount";

        // Build a secret client using the DefaultAzureCredential which uses the managed identity or service principal
        var client = new SecretClient(keyVaultUrl, new DefaultAzureCredential());

        // Retrieve a secret
        KeyVaultSecret secret = await client.GetSecretAsync(secretName);
        string secretValue = secret.Value;

            // Return the secret value
            return secretValue;

        }
        catch (Exception ex)
        {

            return "an error occured receiving secrets => " + ex.Message;
        }
    }


    public static async Task<string> SetAzureDatabaseConnectionccountKey()
    {
        string s = "";

        // Replace <Your-Key-Vault-URL> with the URL of your Key Vault
        // Replace <Your-Secret-Name> with the name of the secret that holds the storage account connection string
        try
        {

        var keyVaultUrl = new Uri("https://kevault-west-europe.vault.azure.net/");
        var secretName = "AzureConnectinoStringDataBase";

        // Build a secret client using the DefaultAzureCredential which uses the managed identity or service principal
        var client = new SecretClient(keyVaultUrl, new DefaultAzureCredential());

        // Retrieve a secret
        KeyVaultSecret secret = await client.GetSecretAsync(secretName);
        string secretValue = secret.Value;

            // Return the secret value
            return secretValue;
        }
        catch (Exception ex)
        {

            return "an error occured receiving secrets => " + ex.Message;
        }
    }

    public static async Task<string> SetEmailPassword()
    {
        // Replace <Your-Key-Vault-URL> with the URL of your Key Vault
        // Replace <Your-Secret-Name> with the name of the secret that holds the storage account connection string
        try
        {

       
        var keyVaultUrl = new Uri("https://kevault-west-europe.vault.azure.net/");
        var secretName = "SystemPasswordEmail";

        // Build a secret client using the DefaultAzureCredential which uses the managed identity or service principal
        var client = new SecretClient(keyVaultUrl, new DefaultAzureCredential());

        // Retrieve a secret
        KeyVaultSecret secret = await client.GetSecretAsync(secretName);
        string secretValue = secret.Value;

        // Return the secret value
        return secretValue;
        }
        catch (Exception ex)
        {

            return "an error occured receiving secrets => " + ex.Message;
        }
    }

    public static async Task<string> SetMainJWTTokenKey()
    {
        // Replace <Your-Key-Vault-URL> with the URL of your Key Vault
        // Replace <Your-Secret-Name> with the name of the secret that holds the storage account connection string
        try
        {

      
        var keyVaultUrl = new Uri("https://kevault-west-europe.vault.azure.net/");
        var secretName = "SystemSecretKeyJwtEncrption";

        // Build a secret client using the DefaultAzureCredential which uses the managed identity or service principal
        var client = new SecretClient(keyVaultUrl, new DefaultAzureCredential());

        // Retrieve a secret
        KeyVaultSecret secret = await client.GetSecretAsync(secretName);
        string secretValue = secret.Value;

        // Return the secret value
        return secretValue;
        }
        catch (Exception ex)
        {
            return "an error occured receiving secrets => " + ex.Message;
        }
    }

    public static async Task<string> SetExtraJwtTokenEncryption()
    {
        // Replace <Your-Key-Vault-URL> with the URL of your Key Vault
        // Replace <Your-Secret-Name> with the name of the secret that holds the storage account connection string
        try
        {
            string changes = "k";
       

        var keyVaultUrl = new Uri("https://kevault-west-europe.vault.azure.net/");
        var secretName = "SystemSecretKeySecurelyExtraEncryptJwtTokenHex";

        // Build a secret client using the DefaultAzureCredential which uses the managed identity or service principal
        var client = new SecretClient(keyVaultUrl, new DefaultAzureCredential());

        // Retrieve a secret
        KeyVaultSecret secret = await client.GetSecretAsync(secretName);
        string secretValue = secret.Value;

        // Return the secret value
        return secretValue;
        }
        catch (Exception ex)
        {
            return "an error occured receiving secrets => " + ex.Message;
        }
    }

    

    public static async Task<string> SetStripeKey()
    {
        // Replace <Your-Key-Vault-URL> with the URL of your Key Vault
        // Replace <Your-Secret-Name> with the name of the secret that holds the storage account connection string
        try
        {



            var keyVaultUrl = new Uri("https://kevault-west-europe.vault.azure.net/");
            var secretName = "StripeSecretKey";

            // Build a secret client using the DefaultAzureCredential which uses the managed identity or service principal
            var client = new SecretClient(keyVaultUrl, new DefaultAzureCredential());
            
            // Retrieve a secret
            KeyVaultSecret secret = await client.GetSecretAsync(secretName);
            string secretValue = secret.Value;

            // Return the secret value
            return secretValue;
        }
        catch (Exception ex)
        {
            return "an error occured receiving secrets => " + ex.Message;
        }
    }






}

