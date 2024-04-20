
using System.Net.Http;

public interface ISecretsConfiguration
{

    string AzureConnectionStringStorageAccountKey { get; }


    string MainJWTTokenKey { get; }
    string ExtraJwtTokenEncryption { get; }
    string FFMPEGExecutable_path { get; set; }

    Task LoadSecretsAsync();
}
public class SecretsConfiguration : ISecretsConfiguration
{
 

    public SecretsConfiguration()
    {
     
    }

    public string AzureConnectionStringStorageAccountKey { get; private set; }
    public string MainJWTTokenKey { get; private set; }
    public string ExtraJwtTokenEncryption { get; private set; }
    public string FFMPEGExecutable_path { get;  set; }



    public void runfunction()
    {

    }

    //Correctly declare AlreadyLoading with a private setter; it defaults to false.




    public async Task LoadSecretsAsync()
    {

        // Start all tasks concurrently
        Task<string> azureConnectionStringStorageAccountKeyTask = AzureKeyVaultServices.SetAzureConnectionStringStorageAccountKey();
        Task<string> mainJWTTokenKeyTask = AzureKeyVaultServices.SetMainJWTTokenKey();
        Task<string> extraJwtTokenEncryptionTask = AzureKeyVaultServices.SetExtraJwtTokenEncryption();

     
        // Await all tasks to complete
        await Task.WhenAll(
            azureConnectionStringStorageAccountKeyTask,
            mainJWTTokenKeyTask,
            extraJwtTokenEncryptionTask
        );

        // Assign results to corresponding properties
        AzureConnectionStringStorageAccountKey = await azureConnectionStringStorageAccountKeyTask;
        MainJWTTokenKey = await mainJWTTokenKeyTask;
        ExtraJwtTokenEncryption = await extraJwtTokenEncryptionTask;



    }


}
