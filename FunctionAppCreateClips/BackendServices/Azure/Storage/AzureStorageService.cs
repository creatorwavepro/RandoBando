using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;


public interface IAzureStorageService
{
    Task<Uri> UploadVideoToBlobAsync(string videoPath, string videoName, string containerName);
    Task<string> DownloadBlobAsTempFileAsync(Uri blobUri, string containerName);
    Task<string> GenerateBlobSasTokenAsync(Uri blobUri);
    Task<string> DownloadFFMpegExecutable();
    Task<string> DownloadBlobFFMPEG();
    Task<string> DownloadBlobAsTempFileAsyncWithExtension(Uri blobUri, string containerName, string extension);
    Task<Uri> UploadImageFromStream(Stream imageStream, string blobName, string containerName);
    Task<Uri> UploadVideoToBlobAsyncInChunks(string localFilePath, string blobName, string containerName);

}

public class AzureStorageService : IAzureStorageService
{
    private readonly ISecretsConfiguration _secretsConfiguration;

    public AzureStorageService(ISecretsConfiguration secretsConfiguration)
    {
        _secretsConfiguration = secretsConfiguration;
    }

    public async Task<Uri> UploadImageFromStream(Stream imageStream, string blobName, string containerName)
    {
        BlobServiceClient blobServiceClient = new BlobServiceClient(_secretsConfiguration.AzureConnectionStringStorageAccountKey);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        // Check if the container exists
        if (!await containerClient.ExistsAsync())
        {
            return null;
        }

        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        const int maxRetries = 3; // Maximum number of retry attempts
        int attempt = 0; // Current attempt
        TimeSpan delay = TimeSpan.FromSeconds(5); // Delay between retries

        while (attempt < maxRetries)
        {
            try
            {
                // Ensure the stream is at the beginning before attempting to upload
                if (imageStream.Position > 0)
                {
                    imageStream.Position = 0;
                }

                // Attempt to upload the stream directly to Azure Blob Storage
                await blobClient.UploadAsync(imageStream, overwrite: true);

                // If upload is successful, return the URI of the uploaded blob
                return blobClient.Uri;
            }
            catch (Exception ex)
            {
                // Log the error. Adjust logging according to your application's logging framework or policy.
                Console.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");

                // Increment the attempt counter
                attempt++;

                // If max attempts have been reached, break out of the loop
                if (attempt == maxRetries)
                {
                    break;
                }

                // Wait before retrying
                await Task.Delay(delay);
            }
        }

        // If all attempts fail, log and return null. Adjust logging according to your application's logging framework.
        Console.WriteLine("All attempts to upload the image have failed.");
        return null;
    }








    public async Task<Uri> UploadVideoToBlobAsyncInChunks(string localFilePath, string blobName, string containerName)
    {
        const int blockSize = 4 * 1024 * 1024; // 4 MB, adjust as needed
        int retryCount = 0;
        int maxRetries = 3;
        TimeSpan delayBetweenRetries = TimeSpan.FromSeconds(10); // Wait 10 seconds between retries

        while (retryCount < maxRetries)
        {
            try
            {
                if (!File.Exists(localFilePath))
                {
                    Console.WriteLine("File does not exist.");
                    throw new FileNotFoundException("File does not exist.", localFilePath);
                }

                BlobServiceClient blobServiceClient = new BlobServiceClient(_secretsConfiguration.AzureConnectionStringStorageAccountKey);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                if (!await containerClient.ExistsAsync())
                {
                    Console.WriteLine($"Container '{containerName}' does not exist.");
                    throw new Exception($"Container '{containerName}' does not exist.");
                }

                BlobClient blobClient = containerClient.GetBlobClient(blobName);
                var blockBlob = containerClient.GetBlockBlobClient(blobName);

                var blockIds = new List<string>();
                long fileSize = new FileInfo(localFilePath).Length;
                long startPosition = 0;
                long bytesUploaded = 0;

                using (var fileStream = File.OpenRead(localFilePath))
                {
                    int index = 0;
                    while (bytesUploaded < fileSize)
                    {
                        int bytesRead = (int)Math.Min(blockSize, fileSize - bytesUploaded);
                        byte[] bytes = new byte[bytesRead];
                        fileStream.Read(bytes, 0, bytesRead);

                        var blockId = Convert.ToBase64String(BitConverter.GetBytes(index));
                        await blockBlob.StageBlockAsync(blockId, new MemoryStream(bytes), null);
                        blockIds.Add(blockId);

                        startPosition += bytesRead;
                        bytesUploaded += bytesRead;
                        index++;
                    }
                }

                await blockBlob.CommitBlockListAsync(blockIds);
                return blockBlob.Uri;
            }
            catch (Exception ex)
            {
                retryCount++;
                Console.WriteLine($"An error occurred uploading to Blob Storage: {ex.Message}. Retrying... Attempt {retryCount} of {maxRetries}");

                if (retryCount >= maxRetries)
                {
                    Console.WriteLine("Maximum retry attempts reached. Failing operation.");
                    throw; // Rethrow the last exception to the caller if max retries reached
                }

                await Task.Delay(delayBetweenRetries);
            }
        }

        return null; // This line is only reached if all retries fail, consider throwing an exception instead
    }







    public async Task<Uri> UploadVideoToBlobAsync(string localFilePath, string blobName, string containerName)
    {
        int retryCount = 0;
        int maxRetries = 3;
        TimeSpan delayBetweenRetries = TimeSpan.FromSeconds(10); // Wait 10 seconds between retries

        while (retryCount < maxRetries)
        {
            try
            {
                // Check if the local file exists
                if (!File.Exists(localFilePath))
                {
                    Console.WriteLine("File does not exist.");
                    return null;
                }

                // Create a BlobServiceClient object with your Azure Storage account's connection string
                BlobServiceClient blobServiceClient = new BlobServiceClient(_secretsConfiguration.AzureConnectionStringStorageAccountKey);

                // Get a reference to the container client object
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                // Ensure the container exists
                if (!await containerClient.ExistsAsync())
                {
                    Console.WriteLine($"Container '{containerName}' does not exist.");
                    return null;
                }

                // Get a reference to a blob
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                // Open a file stream for the local file and upload it directly to Azure Blob Storage
                await using (var uploadFileStream = File.OpenRead(localFilePath))
                {
                    await blobClient.UploadAsync(uploadFileStream, overwrite: true);
                }

                // Return the URI of the uploaded blob
                return blobClient.Uri;
            }
            catch (Exception ex)
            {
                retryCount++;
                await Console.Out.WriteLineAsync($"An error occurred uploading to Blob Storage: {ex.Message}. Retrying... Attempt {retryCount} of {maxRetries}");

                if (retryCount >= maxRetries)
                {
                    // If we've reached the max retries, return null or throw an exception
                    Console.WriteLine("Maximum retry attempts reached. Failing operation.");
                    return null;
                }

                // Wait for a bit before retrying
                await Task.Delay(delayBetweenRetries);
            }
        }

        return null; // This line is only reached if all retries fail
    }

    public async Task<string> DownloadBlobAsTempFileAsyncWithExtension(Uri blobUri, string containerName, string extension)
    {
        // Extract the blob name from the URI
        string blobName = WebUtility.UrlDecode(blobUri.Segments.Last());

        // Create a new BlobClient from the provided connection string, container name, and blob name
        BlobClient blobClient = new BlobClient(_secretsConfiguration.AzureConnectionStringStorageAccountKey, containerName, blobName);

        // Generate a unique file path for the temporary file
        string tempFilePath = Path.GetTempFileName();
        tempFilePath = Path.ChangeExtension(tempFilePath, extension);

        // Initialize retry parameters
        const int maxRetries = 3;
        TimeSpan delay = TimeSpan.FromSeconds(2); // Initial delay of 2 seconds.

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                // Attempt to download the blob to the temporary file
                await blobClient.DownloadToAsync(tempFilePath);
                // If successful, exit the loop
                return tempFilePath;
            }
            catch (Exception ex)
            {
                // Log the exception message or take other appropriate actions
                Console.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
                if (attempt == maxRetries - 1)
                {
                    // If this was the last attempt, rethrow the exception
                    throw;
                }
                // Wait for the delay period before the next attempt
                await Task.Delay(delay);

                // Exponentially increase the delay for the next retry
                delay *= 2;
            }
        }

        // This line is technically unreachable due to the throw in the catch block,
        // but it's required to satisfy the compiler's return path check.
        // Consider adjusting the logic if there's a default action you'd prefer to take when all retries fail.
        return tempFilePath;
    }


    public async Task<string> DownloadBlobAsTempFileAsync(Uri blobUri, string containerName)
    {
        string blobName = WebUtility.UrlDecode(blobUri.Segments.Last());
        BlobClient blobClient = new BlobClient(_secretsConfiguration.AzureConnectionStringStorageAccountKey, containerName, blobName);
        string tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        int maxRetries = 3; // Maximum number of retries
        int attempt = 0; // Current attempt counter
        TimeSpan delay = TimeSpan.FromSeconds(2); // Initial delay between retries, will exponentially increase

        while (attempt < maxRetries)
        {
            try
            {
                await blobClient.DownloadToAsync(tempFilePath);
                return tempFilePath; // Success, return the downloaded file path
            }
            catch (Exception ex)
            {
                attempt++; // Increment the attempt counter
                if (attempt >= maxRetries)
                {
                    // If all retries have been attempted, rethrow the exception to signal failure
                    throw new Exception($"Failed to download blob after {maxRetries} attempts.", ex);
                }

                // Wait for the specified delay before the next retry attempt
                await Task.Delay(delay);
                delay *= 2; // Exponential backoff for the next attempt's delay
            }
        }

        // This line is technically unreachable due to the throw in the catch block
        // but is required to satisfy the compiler's return path checks.
        return null;
    }

    public async Task<string> DownloadBlobFFMPEG()
    {
        Uri ffmpegUri = new Uri("https://socialflowstorageaccount.blob.core.windows.net/finalfiles/ffmpeg.exe?sp=r&st=2024-03-17T15:26:46Z&se=2032-03-21T23:26:46Z&spr=https&sv=2022-11-02&sr=b&sig=hsUgCOpkkteSiMWNerohQm4l23zsMuHSf%2Bzukg6U0EE%3D");

        // Extract the blob name from the URI
        string blobName = WebUtility.UrlDecode(ffmpegUri.Segments.Last());

        // Create a new BlobClient from the provided connection string, container name, and blob name
        BlobClient blobClient = new BlobClient(_secretsConfiguration.AzureConnectionStringStorageAccountKey, "finalfiles", blobName);

        // Generate a unique directory name for the new folder
        string uniqueDirectoryName = Path.GetTempFileName();
        Directory.CreateDirectory(uniqueDirectoryName); // This ensures the directory is created

        // Combine the unique directory with the blob name to get the full file path
        string ffmpegFilePath = Path.Combine(uniqueDirectoryName, blobName);

        // Download the blob to the ffmpegFilePath
        await blobClient.DownloadToAsync(ffmpegFilePath);

        // Return the full path to ffmpeg.exe
        return ffmpegFilePath;
    }



    public async Task<string> DownloadFFMpegExecutable()
    {
        string containerName = "finalfiles";
        Uri blobUri = new Uri("https://socialflowstorageaccount.blob.core.windows.net/finalfiles/ffmpeg.exe?sp=r&st=2024-03-08T14:47:14Z&se=2033-06-01T21:47:14Z&spr=https&sv=2022-11-02&sr=b&sig=c01Q2Gl481tRR43ZkVXK5%2B%2Fyqv53cg2LfzthLoSLJwM%3D");

        // Extract the blob name from the URI
        string blobName = WebUtility.UrlDecode(blobUri.Segments.Last());

        // Create a new BlobClient from the provided connection string, container name, and blob name
        BlobClient blobClient = new BlobClient(_secretsConfiguration.AzureConnectionStringStorageAccountKey, containerName, blobName);

        // Generate a unique file path for the temporary file
        string tempFilePath = Path.GetTempFileName();
        tempFilePath = Path.ChangeExtension(tempFilePath, ".exe");

        // Download the blob to the temporary file
        await blobClient.DownloadToAsync(tempFilePath);

        // Return the path to the temporary file
        return tempFilePath;
    }

    public async Task<string> GenerateBlobSasTokenAsync(Uri blobUri)
{
        if (blobUri == null)
        {
            await Console.Out.WriteLineAsync("blob is null");
            return null;
        }
    // Assuming _connectionString is your Azure Storage account connection string
    BlobServiceClient blobServiceClient = new BlobServiceClient(_secretsConfiguration.AzureConnectionStringStorageAccountKey);

    // Extract the container name and blob name from the blobUri
    var blobContainerName = blobUri.Segments[1].TrimEnd('/');
    var blobName = string.Join("", blobUri.Segments.Skip(2));

    // Get a reference to the blob container and blob
    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
    BlobClient blobClient = containerClient.GetBlobClient(blobName);

    // Set the expiry time and permissions for the SAS. Here, we're giving read permissions and setting it to expire in 1 day.
    BlobSasBuilder sasBuilder = new BlobSasBuilder()
    {
        BlobContainerName = blobContainerName,
        BlobName = blobName,
        Resource = "b", // "b" for blob
        StartsOn = DateTimeOffset.UtcNow,
        ExpiresOn = DateTimeOffset.UtcNow.AddDays(365)
    };

    sasBuilder.SetPermissions(BlobSasPermissions.Read);

    // Use the key to get the SAS token
    var sasToken = blobClient.GenerateSasUri(sasBuilder).Query;

    return sasToken;
}




}
