
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.SoundFont;
using System.Net.Http;
using Microsoft.AspNetCore.WebUtilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System.Data;
using Newtonsoft.Json;

using Microsoft.Extensions.Logging;


public interface IStabilityAIServices
{
    Task<string> GenerateImageStabilityAISingle(string inputText, string style);
}

public class StabilityAIServices : IStabilityAIServices
{

    private readonly IAzureStorageService _azurestorageservice;
    private readonly HttpClient _httpClient;

    private readonly ISecretsConfiguration _secretsConfiguration;

    private readonly ILogger<StabilityAIServices> _logger;

    public StabilityAIServices(ILogger<StabilityAIServices> logger, IHttpClientFactory httpClientFactory, IAzureStorageService azureStorageService, ISecretsConfiguration secretsConfiguration)
    {
        _httpClient = httpClientFactory.CreateClient("MyCustomHttpClient");
        _azurestorageservice = azureStorageService;
        _secretsConfiguration = secretsConfiguration;
        _logger = logger;
    }
    public async Task<string> GenerateImageStabilityAISingle(string inputText, string style)
    {
        string apiUrl = "https://api.stability.ai/v1/generation/stable-diffusion-xl-1024-v1-0/text-to-image";
        string FinalCorrectImageUrl = string.Empty;
        int attempt = 0;
        const int maxAttempts = 5;
        int waitTime = 2; // Initial wait time in seconds

        style = style.ToLower();

        _logger.LogWarning($"stability ai, inputtext : <begin>{inputText}<end> ,   style : <begin>{style}<end>  ");

        int height = 0, width = 0;

     
            height = 768;
            width = 1344;
        
    



        while (attempt < maxAttempts && string.IsNullOrEmpty(FinalCorrectImageUrl))
        {
            try
            {
                
                await Console.Out.WriteLineAsync("making image for keyword : " + inputText);

                var requestData = new
                {
                    text_prompts = new[] { new { text = inputText } },
                    cfg_scale = 30,
                    style_preset = style,
                    height = height,
                    width = width,
                    samples = 1,
                    steps = 50
                };

                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestData), System.Text.Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretsConfiguration.StabilityAIAPIKey);
                HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseJSON = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (responseJSON.ValueKind != JsonValueKind.Undefined && responseJSON.ValueKind != JsonValueKind.Null && responseJSON.TryGetProperty("artifacts", out var artifacts) && artifacts.ValueKind == JsonValueKind.Array && artifacts.GetArrayLength() > 0)
                    {
                        var firstArtifact = artifacts.EnumerateArray().First();

                        if (firstArtifact.TryGetProperty("base64", out var base64Property))
                        {
                            Console.WriteLine("image was received correcyly");

                            // Inside your loop, after getting the base64ImageData
                            var base64ImageData = base64Property.GetString();
                            // Convert base64 string to byte array
                            byte[] imageBytes = Convert.FromBase64String(base64ImageData);
                            // Create a MemoryStream from the byte array
                            using (var imageStream = new MemoryStream(imageBytes))
                            {
                                string name = $"image-{Guid.NewGuid()}.webp";
                                // Now, pass the MemoryStream instead of the string
                                Uri uri = await _azurestorageservice.UploadImageFromStream(imageStream, name, "finalfiles");
                                if (uri != null)
                                {
                                    string sas = await _azurestorageservice.GenerateBlobSasTokenAsync(uri);
                                    FinalCorrectImageUrl = $"{uri}{sas}";

                                    await Console.Out.WriteLineAsync("image made : " + FinalCorrectImageUrl);
                                }
                                else
                                {
                                    Console.Out.WriteLineAsync($"Upload failed for attempt {attempt + 1}. Retrying...");
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("image wasnt received");
                }

            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync("An error occurred when creating images: " + ex.Message);
            }

            if (string.IsNullOrEmpty(FinalCorrectImageUrl))
            {
                await Task.Delay(waitTime * 1000);
                waitTime *= 2; // Exponential backoff
                attempt++;
            }
        }

        return FinalCorrectImageUrl;
    }


    public async Task<string> GenerateImageStabilityAIBeta(string inputText)
    {
        string apiUrl = "https://api.stability.ai/v2beta/stable-image/generate/core";
        string finalCorrectImageUrl = string.Empty;
        int attempt = 0;
        const int maxAttempts = 5;
        var waitTime = 1; // Initial wait time in seconds

        while (attempt < maxAttempts && string.IsNullOrEmpty(finalCorrectImageUrl))
        {
            try
            {
                using var formData = new MultipartFormDataContent();

                // Add the prompt and output format to the form data
                formData.Add(new StringContent(inputText), "an image of john cena");
                formData.Add(new StringContent("webp"), "output_format");

                // Set authorization header
                string apiKey = _secretsConfiguration.StabilityAIAPIKey;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // Set accept header to get the image as base64 encoded JSON
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Make the POST request
                HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, formData);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObject = System.Text.Json.JsonSerializer.Deserialize<dynamic>(responseContent);

                    // Extract base64 image data
                    string base64ImageData = responseObject.image;
                    byte[] imageBytes = Convert.FromBase64String(base64ImageData);

                    // Save or process the image as required
                    using (var imageStream = new MemoryStream(imageBytes))
                    {
                        string name = $"image-{Guid.NewGuid()}.webp";
                        // Now, pass the MemoryStream instead of the string
                        Uri uri = await _azurestorageservice.UploadImageFromStream(imageStream, name, "finalfiles");
                        if (uri != null)
                        {
                            string sas = await _azurestorageservice.GenerateBlobSasTokenAsync(uri);
                            finalCorrectImageUrl = $"{uri}{sas}";

                            await Console.Out.WriteLineAsync("image made : " + finalCorrectImageUrl);
                        }
                        else
                        {
                            Console.Out.WriteLineAsync($"Upload failed for attempt {attempt + 1}. Retrying...");
                        }
                    }
                }
                else
                {
                    await Console.Out.WriteLineAsync($"API request failed with status code {response.StatusCode}. Retrying...");
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync("An error occurred when creating images: " + ex.Message);
            }

            if (string.IsNullOrEmpty(finalCorrectImageUrl))
            {
                await Task.Delay(waitTime * 1000);
                waitTime *= 2; // Exponential backoff
                attempt++;
            }
        }

        return finalCorrectImageUrl;
    }







}


