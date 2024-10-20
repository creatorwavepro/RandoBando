using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Accord.Statistics.Kernels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

namespace FunctionAppCreateClips
{
    public class Ffmpeg
    {
        private readonly IJwtTokenServices _jwtTokenServices;
        private readonly IVideoCreationServices _videoCreationServices;
        private readonly ISecretsConfiguration _secretsConfiguration;
        private readonly IAzureStorageService _azureStorageService;
        private readonly IStabilityAIServices _stabilityAIServices;

        private readonly IVideoCreationServices _videoServices; // Add IVideoServices for trimming


        private readonly ILogger<Ffmpeg> _logger;
        private readonly HttpClient _httpClient;

        private readonly string _pexelsApiKey = "F4UkhEdS28ppZ6BRw34OMvVMAvtSmclCBjS5iDH9eTzkdvVgluYlw1L0";

        public Ffmpeg(ILogger<Ffmpeg> logger,System.Net.Http.IHttpClientFactory httpClientFactory,IJwtTokenServices jwtTokenServices, IStabilityAIServices stabilityAIServices , IVideoCreationServices videoCreationServices, ISecretsConfiguration secretsConfiguration, IAzureStorageService azureStorageService)
        {
            _httpClient = httpClientFactory.CreateClient("MyCustomHttpClient");
            _jwtTokenServices = jwtTokenServices;
            _videoCreationServices = videoCreationServices; 
            _secretsConfiguration = secretsConfiguration;
            _azureStorageService = azureStorageService;
            _stabilityAIServices = stabilityAIServices;
            _logger = logger;
        }



        //private readonly ILogger _logger;

        //public Ffmpeg(ILoggerFactory loggerFactory)
        //{
        //    _logger = loggerFactory.CreateLogger<Ffmpeg>();
        //}

        [Function("CreateClips")]
        public async Task<IActionResult> CreateClips([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CreateClipsClass input = JsonSerializer.Deserialize<CreateClipsClass>(requestBody);

            string limits = "sqdsdbgqsqfdsdss";

            if (input == null)
            {
                return new BadRequestObjectResult("An error occurred. The request body could not be deserialized.");
            }

      


            if (! await _jwtTokenServices.IsValidJWTAsync(input.jwt))
            {
                return new UnauthorizedObjectResult("invalid token " + "invalid token " + "called create clips with token =>  " + input.jwt + "jwt token secret : " + _secretsConfiguration.MainJWTTokenKey + "keyvault url :  " + Environment.GetEnvironmentVariable("AzureKeyvaultUrl")) ;
            }

          

            string path = null;
            int retries = 0;
            int maxRetries = 3;
            while (retries < maxRetries && (string.IsNullOrEmpty(path) || !path.Contains("http")))
            {


                path = await _videoCreationServices.GenerateVideoFromImageAndStoreLocally(input.FfmpegCommand) ;

                if (!string.IsNullOrEmpty(path) && path.Contains("http"))
                {
                    break;
                }

                retries++;
                // Exponential backoff formula: 2^retries * 100 milliseconds
                int delay = (int)Math.Pow(2, retries) * 100;
                await Task.Delay(delay);
            }

            if (!string.IsNullOrEmpty(path) && path.Contains("http"))
            {
                return new OkObjectResult(path);
            }
            else
            {
                return new BadRequestObjectResult("Failed to create clip after retries.");
            }
        }

        public class CreateClipsClass
        {
            // JWT Token for verification
            public string FfmpegCommand { get; set; }
            public string jwt { get; set; }




            public CreateClipsClass()
            {

            }

        }


        [Function("JoinClips")]
        public async Task<IActionResult> JoinClips([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            JoinClipssClass input = JsonSerializer.Deserialize<JoinClipssClass>(requestBody);

            if (input == null)
            {
                return new BadRequestObjectResult("An error occurred. The request body could not be deserialized.");
            }

   

            if (!await _jwtTokenServices.IsValidJWTAsync(input.jwt))
            {
                return new UnauthorizedObjectResult("invalid token " + "called create clips with token =>  " + input.jwt + "jwt token secret : " + _secretsConfiguration.MainJWTTokenKey);
            }

            // Prepare to download each video file to a local file, all simultaneously
            var downloadTasks = input.cliplist.Select(clipUri =>
                _azureStorageService.DownloadBlobAsTempFileAsync(new Uri(clipUri), "finalfiles")
            ).ToList();

            List<string> localVideoPaths;
            try
            {
                localVideoPaths = (await Task.WhenAll(downloadTasks)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading files: {ex.Message}");
                return new BadRequestObjectResult($"Failed to download one or more clips: {ex.Message}");
            }

            if (localVideoPaths.Count == 0)
            {
                return new BadRequestObjectResult("No videos were downloaded successfully.");
            }

            string path = null;
            int retries = 0;
            int maxRetries = 3;
            int delayInSeconds = 5;
            bool joinSuccessful = false;

            while (retries < maxRetries && !joinSuccessful)
            {
                try
                {
                    path = await _videoCreationServices.JoinVideoStoreInCloud(localVideoPaths);
                    if (!string.IsNullOrEmpty(path) && path.Contains("http"))
                    {
                        joinSuccessful = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {retries + 1} to join videos failed: {ex.Message}");
                    if (retries >= maxRetries - 1)
                    {
                        // If it was the last retry attempt, exit the loop
                        break;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));
                }
                retries++;
            }

            // Cleanup: Delete downloaded files after attempting to join
            foreach (var filePath in localVideoPaths)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
              
            }

            if (joinSuccessful)
            {
                return new OkObjectResult(path);
            }
            else
            {
                return new BadRequestObjectResult("Failed to create clip after retries.");
            }
        }

        public class JoinClipssClass
        {
            // JWT Token for verification
            public List<string> cliplist { get; set; }
            public string jwt { get; set; }
            public JoinClipssClass()
            {

            }

        }



     




        [Function("ReplaceAudio")]
        public async Task<IActionResult> ReplaceAudio([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            ReplaceAudioClass input = JsonSerializer.Deserialize<ReplaceAudioClass>(requestBody);

            if (input == null)
            {
                return new BadRequestObjectResult("An error occurred. The request body could not be deserialized.");
            }

            if (!await _jwtTokenServices.IsValidJWTAsync(input.jwt))
            {
                return new UnauthorizedObjectResult("invalid token");
            }

            string path = null;
            int retries = 0;
            int maxRetries = 3;
            while (retries < maxRetries && (string.IsNullOrEmpty(path) || !path.Contains("http")))
            {


                path = await _videoCreationServices.ReplaceAudioInVideo(input.mergedvideo, input.mergedaudio);

                if (!string.IsNullOrEmpty(path) && path.Contains("http"))
                {
                    break;
                }

                retries++;
                // Exponential backoff formula: 2^retries * 100 milliseconds
                int delay = (int)Math.Pow(2, retries) * 100;
                await Task.Delay(delay);
            }

            if (!string.IsNullOrEmpty(path) && path.Contains("http"))
            {
                return new OkObjectResult(path);
            }
            else
            {
                return new BadRequestObjectResult("Failed to create clip after retries.");
            }
        }

        public class ReplaceAudioClass
        {
          
            public string mergedvideo { get; set; }
            public string mergedaudio { get; set; }

            public string jwt { get; set; }


            public ReplaceAudioClass()
            {

            }

        }


        [Function("WakeFunction")]
        public async Task<IActionResult> WakeFunction([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {

            return new OkObjectResult("function has awoken");
          
        }




        public class ShortRequestModel
        {
            // JWT Token for verification
            public string Token { get; set; }
            public string ClipPath { get; set; }
            public string Audio { get; set; }

            public int start { get; set; }
            public int end { get; set; }

            public ShortRequestModel()
            {

            }
        }








        // STABILITY AI 

        [Function("StabilityAIEndpoint")]
        public async Task<IActionResult> StabilityAIEndpoint([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            string s = "";
            _logger.LogInformation("C# HTTP trigger function processing a request.");
            _logger.LogWarning("st password : " + _secretsConfiguration.StabilityAIAPIKey);

            // Extract query parameters (or body data if you prefer POST)
            string keyword = req.Query["keyword"];
            string styleParam = req.Query["style"]; // Optionally, include a style parameter if you want specific styling

            styleParam = "anime";

            if (string.IsNullOrEmpty(keyword))
            {
                return new BadRequestObjectResult("Please provide a keyword.");
            }

            // If no style is provided, default to a general style
            string style = string.IsNullOrEmpty(styleParam) ? "none" : styleParam;

            // Call the StabilityAI service to generate an image
            string generatedImageUrl;
            try
            {
                generatedImageUrl = await _stabilityAIServices.GenerateImageStabilityAISingle(keyword, style);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Failed to generate image from Stability AI: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            if (string.IsNullOrEmpty(generatedImageUrl))
            {
                return new NotFoundObjectResult("No image could be generated.");
            }

            return new OkObjectResult(generatedImageUrl);
        }



































        // PEXEL MEDIA FINDER


        [Function("PexelMediaFinder")]
        public async Task<IActionResult> PexelMediaFinder([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processing a request.");

            // Extract query parameters (or body data if you prefer POST)
            string keyword = req.Query["keyword"];
            string isVideoParam = req.Query["video"];
            string horizontalParam = req.Query["horizontal"];
            string durationParam = req.Query["duration"];

            if (string.IsNullOrEmpty(keyword))
            {
                return new BadRequestObjectResult("Please provide a keyword.");
            }

            // Parse the "video" flag
            bool isVideo = false;
            if (!string.IsNullOrEmpty(isVideoParam) && bool.TryParse(isVideoParam, out bool parsedIsVideo))
            {
                isVideo = parsedIsVideo;
            }

            // Parse the "horizontal" flag
            bool horizontal = true;
            if (!string.IsNullOrEmpty(horizontalParam) && bool.TryParse(horizontalParam, out bool parsedHorizontal))
            {
                horizontal = parsedHorizontal;
            }

            // Parse the "duration" parameter (default to 10 seconds if not provided)
            double duration = 10.0;
            if (!string.IsNullOrEmpty(durationParam) && double.TryParse(durationParam, out double parsedDuration))
            {
                duration = parsedDuration;
            }

            // Call PexelsMedia service to get the first media URL (image or video)
            string mediaUrl;
            try
            {
                mediaUrl = await  GetFirstMediaUrlAsync(keyword, isVideo, horizontal, duration);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Failed to retrieve media from Pexels API: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            if (string.IsNullOrEmpty(mediaUrl))
            {
                return new NotFoundObjectResult("No matching media found.");
            }

            return new OkObjectResult(mediaUrl);
        }



        public async Task<string> GetFirstMediaUrlAsync(string keyword, bool isVideo, bool isHorizontal, double duration)
        {
            // Build the query URL for Pexels API, request videos or photos
            string requestUrl = isVideo
                ? $"https://api.pexels.com/videos/search?query={keyword}&per_page=10"
                : $"https://api.pexels.com/v1/search?query={keyword}&per_page=10";

            // Set up the request headers for Pexels API
            _httpClient.DefaultRequestHeaders.Add("Authorization", _pexelsApiKey);

            // Make the request to Pexels API
            HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch data from Pexels API.");
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(jsonResponse);

            // Extract the media URLs and dimensions
            JArray mediaItems = isVideo ? (JArray)json["videos"] : (JArray)json["photos"];

            foreach (var item in mediaItems)
            {
                string mediaUrl = isVideo ? item["video_files"][0]["link"].ToString() : item["src"]["original"].ToString();
                int width = int.Parse(isVideo ? item["width"].ToString() : item["width"].ToString());
                int height = int.Parse(isVideo ? item["height"].ToString() : item["height"].ToString());

                // Check orientation
                if (isHorizontal && width > height || !isHorizontal && height > width)
                {
                    if (isVideo)
                    {
                        double videoDuration = double.Parse(item["duration"].ToString());

                        // If the video is longer than the specified duration, trim it
                        if (videoDuration > duration)
                        {
                            _logger.LogWarning("Know we have to cut the video");
                            mediaUrl = await TrimAndUploadVideo(mediaUrl, duration, videoDuration);
                            return mediaUrl;
                        }
                        else
                        {
                            // Upload the full video if no trimming is needed
                            string uploadedVideoUrl = await DownloadAndUploadMedia(mediaUrl, isVideo);
                            if (!string.IsNullOrEmpty(uploadedVideoUrl))
                            {
                                return uploadedVideoUrl; // Return the first successfully uploaded media URL
                            }
                        }
                    }
                    else
                    {
                        // For images
                        string uploadedImageUrl = await DownloadAndUploadMedia(mediaUrl, isVideo);
                        if (!string.IsNullOrEmpty(uploadedImageUrl))
                        {
                            return uploadedImageUrl; // Return the first successfully uploaded image URL
                        }
                    }
                }
            }

            return null; // Return null if no media could be downloaded and uploaded
        }

        private async Task<string> DownloadAndUploadMedia(string mediaUrl, bool isVideo)
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"media-{Guid.NewGuid()}." + (isVideo ? "mp4" : "jpg"));

            try
            {
                // Step 1: Download the media locally
                using (HttpResponseMessage response = await _httpClient.GetAsync(mediaUrl))
                {
                    response.EnsureSuccessStatusCode();
                    await using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }

                // Step 2: Upload the media to Azure Blob Storage
                string blobName = $"media-{Guid.NewGuid()}." + (isVideo ? "mp4" : "jpg");
                await using (var mediaStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                {
                    Uri blobUri = isVideo
                        ? await _azureStorageService.UploadVideoToBlobAsync(tempFilePath, blobName, "finalfiles")
                        : await _azureStorageService.UploadImageFromStream(mediaStream, blobName, "finalfiles");

                    if (blobUri != null)
                    {
                        string sasToken = await _azureStorageService.GenerateBlobSasTokenAsync(blobUri);
                        return $"{blobUri}{sasToken}"; // Return the uploaded media URL with SAS token
                    }
                }

                return null; // Return null if upload fails
            }
            catch (Exception ex)
            {
                // Log or handle errors, skip to the next media if any error occurs
                Console.WriteLine($"Error downloading or uploading media: {ex.Message}");
                return null;
            }
            finally
            {
                // Clean up the temporary file after upload or failure
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
            }
        }

        private async Task<string> TrimAndUploadVideo(string videoUrl, double duration, double videoDuration)
        {
            try
            {
                // Use IVideoServices to trim and upload the video
                _logger.LogWarning("calling the function");
                string trimmedVideoUrl = await DownloadAndTrimVideoAsync(videoUrl, (int)duration, videoDuration, "finalfiles");

              
                return trimmedVideoUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error trimming video: {ex.Message}");
                return null;
            }
        }









        public async Task<string> DownloadAndTrimVideoAsync(string videoUrl, int trimDuration,double videoDuration,  string containerName)
        {
            // Step 1: Download the video
            string tempVideoPath = Path.GetTempFileName();
            tempVideoPath = Path.ChangeExtension(tempVideoPath, ".mp4");

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(videoUrl);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to download video.");
                }

                // Save the downloaded video
                await using (var fileStream = new FileStream(tempVideoPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }

            _logger.LogWarning("BEFORE GETTING DURAITON");
            // Step 2: Get the video duration
        

            _logger.LogWarning("AFTER GETTING DURAITON");

            // Step 3: If requested duration exceeds video length, return full video
            string trimmedVideoPath = "";
            if (trimDuration >= videoDuration)
            {
                trimmedVideoPath = tempVideoPath; // No trimming necessary, use the original video path
            }
            else
            {

                _logger.LogWarning("UP TO ACTUAL TRIMMITNG ALL GOOD =>" + trimmedVideoPath);
                // Step 4: Trim the video
                trimmedVideoPath = Path.ChangeExtension(Path.GetTempFileName(), ".mp4");
                string ffmpegArgs = $"-i \"{tempVideoPath}\" -t {trimDuration} -c copy \"{trimmedVideoPath}\"";

                using (Process ffmpegProcess = new Process())
                {
                    ffmpegProcess.StartInfo = new ProcessStartInfo
                    {
                        FileName = _secretsConfiguration.FFMPEGExecutable_path, // Set your FFmpeg path
                        Arguments = ffmpegArgs,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    ffmpegProcess.Start();
                    await ffmpegProcess.WaitForExitAsync();

                    // Check for errors during trimming
                    if (ffmpegProcess.ExitCode != 0)
                    {
                        string error = await ffmpegProcess.StandardError.ReadToEndAsync();
                        throw new Exception($"FFmpeg error during trimming: {error}");
                    }
                }

                // Cleanup the original video after trimming
                if (File.Exists(tempVideoPath))
                {
                    File.Delete(tempVideoPath);
                }
            }


            _logger.LogWarning("AFTER  The  ACTUAL TRIMMITNG ALL GOOD =>" + trimmedVideoPath);

            // Step 5: Set the blobName to "clip-c-{guid.new}.mp4"
            string blobName = $"clip-c-{Guid.NewGuid()}.mp4";

            // Step 6: Upload the trimmed video to Azure Blob Storage
            Uri uploadedUri = await _azureStorageService.UploadVideoToBlobAsync(trimmedVideoPath, blobName, containerName);
            if (uploadedUri == null)
            {
                throw new Exception("Failed to upload video to Azure Blob Storage.");
            }
            _logger.LogWarning("AFTER  The  ACTUAL UPLOADDING  ALL GOOD =>" + trimmedVideoPath);
            // Step 7: Cleanup the trimmed local file
            if (File.Exists(trimmedVideoPath))
            {
                File.Delete(trimmedVideoPath);
            }
            string sas = await _azureStorageService.GenerateBlobSasTokenAsync(uploadedUri);

            string final = uploadedUri.ToString() + sas;
            // Step 8: Return the URI of the uploaded video
            return final;
        }

        // Helper function to get the video duration
     































        // GOOGLE MEDIA FINDER

        [Function("GoogleMediaFinder")]
        public async Task<IActionResult> GoogleMediaFinder([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processing a request.");

            // Extract query parameters (or body data if you prefer POST)
            string keyword = req.Query["keyword"];
            string horizontalParam = req.Query["horizontal"];

            if (string.IsNullOrEmpty(keyword))
            {
                return new BadRequestObjectResult("Please provide a keyword.");
            }

            bool horizontal = true;
            if (!string.IsNullOrEmpty(horizontalParam) && bool.TryParse(horizontalParam, out bool parsedHorizontal))
            {
                horizontal = parsedHorizontal;
            }


            _logger.LogWarning("up to calling function we are good");
            // Call GoogleMedia service to get the first image URL
            string imageUrl = "jhi";
            try
            {
                imageUrl = await GetFirstImageUrlAsync(keyword, horizontal);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("error 123 :> " + ex.Message);
                _logger.LogError($"Failed to retrieve image from Google Custom Search API: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            if (string.IsNullOrEmpty(imageUrl))
            {
                return new NotFoundObjectResult("No matching images found.");
            }

            return new OkObjectResult(imageUrl);


            //return new OkObjectResult("function has awoken");
        }


        private readonly string _apiKey = "AIzaSyDSYFLhl4pnJt-UY-vYN_CBhc7ZTJ-h4IQ";
        private readonly string _searchEngineId = "f5f5221c8ec244d0b";
        public async Task<string> GetFirstImageUrlAsync(string keyword, bool horizontal)
        {
        

            try
            {
                _logger.LogInformation($"Starting Google image search for keyword: {keyword}, horizontal: {horizontal}");

                // Build the query URL for Google Custom Search API (image search), request 10 images
                string requestUrl = $"https://www.googleapis.com/customsearch/v1?q={keyword}&searchType=image&key={_apiKey}&cx={_searchEngineId}&num=10";
                _logger.LogInformation($"Request URL: {requestUrl}");

                // Make the request to Google Custom Search API
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch data from Google Custom Search API. Status code: {response.StatusCode}");
                    throw new HttpRequestException("Failed to fetch data from Google Custom Search API.");
                }

                _logger.LogInformation("Successfully received response from Google API.");

                // Process the response
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(jsonResponse);

                // Extract up to 10 image URLs from the search results
                foreach (var item in json["items"])
                {
                    string imageUrl = item["link"].ToString();
                    int width = int.Parse(item["image"]["width"].ToString());
                    int height = int.Parse(item["image"]["height"].ToString());

                    _logger.LogInformation($"Found image: {imageUrl}, Width: {width}, Height: {height}");

                    // Check if the image matches the orientation (horizontal or vertical)
                    if (horizontal && width > height || !horizontal && height > width)
                    {
                        _logger.LogInformation("Image matches orientation. Attempting to download and upload.");

                        // Try to download and upload the image
                        string uploadedImageUrl = await DownloadAndUploadImage(imageUrl);
                        if (!string.IsNullOrEmpty(uploadedImageUrl))
                        {
                            _logger.LogInformation($"Successfully uploaded image. URL: {uploadedImageUrl}");
                            return uploadedImageUrl; // Return the first successfully uploaded image URL
                        }
                        else
                        {
                            _logger.LogWarning("Image upload failed.");
                        }
                    }
                }

                _logger.LogWarning("No matching images found.");
                return null; // Return null if no image could be downloaded and uploaded
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HttpRequestException occurred: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"General exception occurred: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task<string> DownloadAndUploadImage(string imageUrl)
        {
            string tempFilePath = string.Empty;

            try
            {
                _logger.LogInformation($"Downloading image from: {imageUrl}");

                // Step 1: Download the image locally
                tempFilePath = Path.Combine(Path.GetTempPath(), $"image-{Guid.NewGuid()}.jpg");
                using (HttpResponseMessage response = await _httpClient.GetAsync(imageUrl))
                {
                    response.EnsureSuccessStatusCode();
                    await using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }

                _logger.LogInformation("Image downloaded successfully. Uploading to Azure...");

                // Step 2: Upload the image to Azure Blob Storage
                string blobName = $"image-{Guid.NewGuid()}.jpg";
                await using (var imageStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                {
                    Uri blobUri = await _azureStorageService.UploadImageFromStream(imageStream, blobName, "finalfiles");
                    if (blobUri != null)
                    {
                        string sasToken = await _azureStorageService.GenerateBlobSasTokenAsync(blobUri);
                        string finalImageUrl = $"{blobUri}{sasToken}";
                        _logger.LogInformation($"Image uploaded successfully. URL: {finalImageUrl}");
                        return finalImageUrl; // Return the uploaded image URL with SAS token
                    }
                }

                _logger.LogWarning("Image upload failed.");
                return null; // Return null if upload fails
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading or uploading image: {ex.Message}, StackTrace: {ex.StackTrace}");
                return null;
            }
            finally
            {
                // Step 3: Delete the local file after upload or failure
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                    _logger.LogInformation($"Temporary file deleted: {tempFilePath}");
                }
            }
        }



    }
}
