using System.Net;
using System.Text.Json;
using Accord.Statistics.Kernels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionAppCreateClips
{
    public class Ffmpeg
    {
        private readonly IJwtTokenServices _jwtTokenServices;
        private readonly IVideoCreationServices _videoCreationServices;
        private readonly ISecretsConfiguration _secretsConfiguration;
        private readonly IAzureStorageService _azureStorageService;

        private readonly ILogger<Ffmpeg> _logger;
        private readonly HttpClient _httpClient;

        public Ffmpeg(ILogger<Ffmpeg> logger,System.Net.Http.IHttpClientFactory httpClientFactory,IJwtTokenServices jwtTokenServices,  IVideoCreationServices videoCreationServices, ISecretsConfiguration secretsConfiguration, IAzureStorageService azureStorageService)
        {
            _httpClient = httpClientFactory.CreateClient("MyCustomHttpClient");
            _jwtTokenServices = jwtTokenServices;
            _videoCreationServices = videoCreationServices; 
            _secretsConfiguration = secretsConfiguration;
            _azureStorageService = azureStorageService;
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

            string limits = "sqdsddsdss";

            if (input == null)
            {
                return new BadRequestObjectResult("An error occurred. The request body could not be deserialized.");
            }

      


            if (! await _jwtTokenServices.IsValidJWTAsync(input.jwt))
            {
                return new UnauthorizedObjectResult("invalid token " + "invalid token " + "called create clips with token =>  " + input.jwt + "jwt token secret : " + _secretsConfiguration.MainJWTTokenKey);
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








    }
}
