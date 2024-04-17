
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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System.Data;
using Newtonsoft.Json;
using Google.Protobuf.Reflection;
using System.Security.Policy;


public interface IVideoCreationServices
{
    Task<string> SaveAndJoinAudioSegmentsAsync(List<string> tempFilePaths);
    Task<string> GenerateVideoFromImageAndStoreLocally(string ffmpegCommand);
    Task<string> JoinVideoStoreInCloud(List<string> videoPaths);
    Task<string> ReplaceAudioInVideo(string videoPath, string audioPath);



}

public class VideoCreationServices : IVideoCreationServices
{


    private readonly IAzureStorageService _azurestorageservice;
    private readonly HttpClient _httpClient;

    private readonly ISecretsConfiguration _secretsConfiguration;

    public VideoCreationServices( IHttpClientFactory httpClientFactory, IAzureStorageService azureStorageService, ISecretsConfiguration secretsConfiguration)
    {
        _httpClient = httpClientFactory.CreateClient("MyCustomHttpClient");
        _azurestorageservice = azureStorageService;
        _secretsConfiguration = secretsConfiguration;

    }

    public async Task<string> SaveAndJoinAudioSegmentsAsync(List<string> tempFilePaths)
    {

        string finalAudioPath = "";

        try
        {
            // Call the existing function to join these files
            finalAudioPath = await JoinAudioFilesAsync(tempFilePaths);



            foreach (string item in tempFilePaths)
            {
                if (File.Exists(item))
                {
                    File.Delete(item);
                }
            }

            // Optionally, cleanup the temporary files after joining
            // Be careful with this if JoinAudioFilesAsync moves or deletes the input files.
           
        }
        catch (Exception ex)
        {
            // Handle exceptions (logging, cleanup, etc.)
            throw;
        }

        return finalAudioPath;
    }


    public async Task<string> JoinAudioFilesAsync(List<string> inputFiles)
    {
     

        // Generate a unique temporary file path for the output
        string outputPath = Path.GetTempFileName();
        // Optionally, change the extension to .mp3 if needed
        outputPath = Path.ChangeExtension(outputPath, ".mp3");

       

        string tempFileList = Path.GetTempFileName();
        try
        {
            var lines = inputFiles.Select(file => $"file '{file}'").ToList();
            await File.WriteAllLinesAsync(tempFileList, lines);



            // Build the FFmpeg command
            string args = $"-y -f concat -safe 0 -i {tempFileList} -c copy {outputPath}";


            // Execute the FFmpeg command
            using Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _secretsConfiguration.FFMPEGExecutable_path,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // Asynchronously read the standard output and standard error
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Log output and errors
            if (!string.IsNullOrWhiteSpace(output))
            {
            }
            if (!string.IsNullOrWhiteSpace(error))
            {
            }

            // Check the exit code to determine if FFmpeg was successful
            if (process.ExitCode != 0)
            {
                // Depending on your application's requirements, you might want to throw an exception here
                // throw new Exception($"FFmpeg processing failed with exit code {process.ExitCode}.");
            }

            return outputPath;
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync("an error during audio generation " + ex.Message);

            throw; // Rethrow the exception to handle it further up the call stack
        }
        finally
        {
          
        }






    }

    //public async Task<string> CreateClips(string imagePath, string sentence, double speedPerWord)
    //{
    //    await Console.Out.WriteLineAsync("creating clip for image : " + imagePath);
    //    await Console.Out.WriteLineAsync("creating clip for SENTENCE : " + sentence);
    //    await Console.Out.WriteLineAsync("creating clip for speedPerWord : " + speedPerWord);

    //    try
    //    {
    //        // Validate input parameters
    //        if (string.IsNullOrEmpty(imagePath) || string.IsNullOrEmpty(sentence))
    //        {
    //            Console.WriteLine("Invalid image path or sentence.");
    //            return string.Empty; // Return an empty string if the inputs are not valid
    //        }

    //        // Calculate duration based on the sentence and speed per word
    //        int duration = Math.Max(1, (int)Math.Round(CountWords(sentence) * speedPerWord));

    //        string videoPath = null;
    //        int retryCount = 0;
    //        int maxRetries = 5;
    //        int backoffDelay = 2000; // Initial backoff delay in milliseconds

    //        // Retry logic with exponential backoff
    //        while (string.IsNullOrEmpty(videoPath) && retryCount < maxRetries)
    //        {
    //            if (retryCount > 0)
    //            {
    //                await Console.Out.WriteLineAsync($"Retrying... Attempt {retryCount}");
    //                await Task.Delay(backoffDelay);
    //                backoffDelay *= 2; // Double the backoff delay for the next attempt
    //            }

    //            videoPath = await GenerateVideoFromImageAndStoreLocally(imagePath, duration);
    //            retryCount++;
    //        }

    //        if (!string.IsNullOrEmpty(videoPath))
    //        {
    //            Console.WriteLine("Video has been made: " + videoPath);
    //            return videoPath; // Return the path for the created video
    //        }
    //        else
    //        {
    //            await Console.Out.WriteLineAsync("THERE WAS AN ERROR <while creating video> (((((àààààà)))))))");
    //            return string.Empty; // Return an empty string if an exception occurred
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        // Log the exception details to help with debugging
    //        Console.WriteLine($"An error occurred: {ex.Message}");
    //        return string.Empty; // Return an empty string if an exception occurred
    //    }
    //}



    public int CountWords(string sentence)
    {
        string pattern = @"\b\w+\b";
        Regex regex = new Regex(pattern);

        MatchCollection matches = regex.Matches(sentence);
        return matches.Count;
    }




    //public async Task<string> GenerateVideoFromImageAndStoreLocally(string inputPath, int duration)
    //{
    //    int maxAttempts = 3;
    //    int attempt = 0;

    //    while (attempt < maxAttempts)
    //    {
    //        try
    //        {
    //            // Adjust speed based on duration
    //            int speed = 1;
    //            switch (duration)
    //            {
    //                case 1: speed = 5; break;
    //                case 2: speed = 4; break;
    //                case 3: speed = 3; break;
    //                case 4: speed = 2; break;
    //            }

    //            // Generate a unique file name for the output video in the system's temporary directory
    //            string outputPath = Path.GetTempFileName();
    //            outputPath = Path.ChangeExtension(outputPath, ".mp4");

    //            if (File.Exists(outputPath))
    //            {
    //                File.Delete(outputPath);
    //            }

    //            // Construct the FFmpeg command with the output path
    //            string ffmpegCommand = $"-y -loop 1 -i \"{inputPath}\" -c:v libx264 -t {duration} -pix_fmt yuv420p " +
    //                                   $"-vf \"scale=3840:-2,zoompan=z='min(zoom+0.00{speed},1.5)':d={duration * 50}:x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':fps=50\" " +
    //                                   $"-preset veryslow -crf 30 \"{outputPath}\"";

    //            // Log the command being executed
    //            Console.WriteLine($"Executing FFmpeg command: {ffmpegCommand}");

    //            // Execute FFmpeg command
    //            using (var process = new Process())
    //            {
    //                process.StartInfo.FileName = _secretsConfiguration.FFMPEGExecutable_path; // Ensure FFmpeg is accessible in the system's PATH
    //                process.StartInfo.Arguments = ffmpegCommand;
    //                process.StartInfo.RedirectStandardOutput = true;
    //                process.StartInfo.RedirectStandardError = true;
    //                process.StartInfo.UseShellExecute = false;
    //                process.StartInfo.CreateNoWindow = true;
    //                process.Start();

    //                // Asynchronously read the output to ensure the process does not block
    //                string output = await process.StandardOutput.ReadToEndAsync();
    //                string error = await process.StandardError.ReadToEndAsync();

    //                process.WaitForExit();

    //                if (process.ExitCode != 0)
    //                {
    //                    // Handle error case
    //                    Console.WriteLine($"FFmpeg command failed with exit code {process.ExitCode}. Error: {error}");
    //                    attempt++;
    //                    continue;
    //                }


    //                string uriWithSas = "";
    //                string videoname = $"clips-{Guid.NewGuid()}.mp4";
    //                try
    //                {

    //                    Uri uri = await _azurestorageservice.UploadVideoToBlobAsyncInChunks(outputPath, videoname, "finalfiles");

    //                    string sas = await _azurestorageservice.GenerateBlobSasTokenAsync(uri);

    //                    uriWithSas = $"{uri}{sas}";
    //                }
    //                catch (Exception e)
    //                {
    //                    await Console.Out.WriteLineAsync("An error occurred making FFmpeg request: " + e.Message);
    //                    throw;
    //                }

    //                if (!string.IsNullOrEmpty(uriWithSas))
    //                {
    //                    return uriWithSas;
    //                }

    //                // Command executed successfully, return the output path
    //                return outputPath;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}. Retrying...");
    //            attempt++;
    //            if (attempt >= maxAttempts)
    //            {
    //                Console.WriteLine("Maximum retry attempts reached. Failing operation.");
    //                throw; // Consider handling this case more gracefully
    //            }
    //        }
    //    }

    //    // This line is only reached if all retries fail
    //    throw new Exception("Failed to generate video from image after multiple attempts.");
    //}



    // public async Task<string> GenerateVideoFromImageAndStoreLocally(string inputPath, int duration)
    // {
    //     int maxAttempts = 3;
    //     int attempt = 0;

    //     while (attempt < maxAttempts)
    //     {
    //         try

    //         {

    //             // Adjust speed based on duration
    //             int speed = 1;
    //             switch (duration)
    //             {
    //                 case 1: speed = 28; break;
    //                 case 2: speed = 25; break;
    //                 case 3: speed = 23; break;
    //                 case 4: speed = 2; break;
    //             }

    //             string videoOutputPath = Path.GetTempFileName();
    //             videoOutputPath = Path.ChangeExtension(videoOutputPath, ".mp4");

    //             Random rand = new Random();
    //             int num = rand.Next(0, 4);



    //             string ffmpegCommand = "";

    //             switch (num)
    //             {
    //                 case 0:  // Construct the FFmpeg command with the output path
    //                     ffmpegCommand = $"-y -loop 1 -i \"{inputPath}\" -c:v libx264 -t {duration} -pix_fmt yuv420p " +
    //                                            $"-vf \"scale=3840:-2,zoompan=z='min(zoom+0.00{speed},1.5)':d={duration * 50}:x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':fps=50\" " +
    //                                            $"-preset veryslow -crf 30 \"{videoOutputPath}\""; break;
    //                 case 1:  // Construct the FFmpeg command
    //                     ffmpegCommand = $"-y -loop 1 -i \"{inputPath}\" -c:v libx264 -t {duration} -pix_fmt yuv420p " +
    //                                          $"-vf \"scale=3840:-2,zoompan=z='if(lte(zoom,1.0),1.5,max(1.001,zoom-0.00{speed}))':d={duration * 50}:x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':fps=50\" " +
    //                                          $"-preset veryslow -crf 30 \"{videoOutputPath}\""; break;
    //                 case 2:
    //                     ffmpegCommand = $"-y -loop 1 -i \"{inputPath}\" -c:v libx264 -t {duration} -pix_fmt yuv420p " +
    //                $"-vf \"scale=3840:-2,zoompan=z='if(lte(zoom,1.0),1.5,max(1.001,zoom-0.00{speed})):d={duration * 50}:" +
    //                $"x='if(lte(zoom\\,1.0)\\,0\\,min(iw/2-(iw/zoom/2)\\,((iw/zoom)-iw)*((zoom-1)/(1.5-1))*on/(5*50)))':" + // Correctly escaped for C# string
    //                $"y='ih/2-(ih/zoom/2)':fps=50\" " +
    //                $"-preset veryslow -crf 30 \"{videoOutputPath}\"";
    //                     break;
    //                 case 3:
    //                     ffmpegCommand = $"-y -loop 1 -i \"{inputPath}\" -c:v libx264 -t {duration} -pix_fmt yuv420p " +
    //$"-vf \"scale=3840:-2,zoompan=z='if(lte(zoom,1.0),1.5,max(1.001,zoom-0.00{speed})):d={duration * 50}:" +
    //$"x='if(lte(zoom\\,1.0)\\,iw-iw/zoom\\,max(0\\,(iw/2-(iw/zoom/2))-((iw/zoom)-iw)*((zoom-1)/(1.5-1))*on/(5*50)))':" + // Adjusted for right to left panning
    //$"y='ih/2-(ih/zoom/2)':fps=50\" " +
    //$"-preset veryslow -crf 30 \"{videoOutputPath}\"";
    //                     break;
    //                 default:
    //                     // Construct the FFmpeg command
    //                     ffmpegCommand = $"-y -loop 1 -i \"{inputPath}\" -c:v libx264 -t {duration} -pix_fmt yuv420p " +
    //                                          $"-vf \"scale=3840:-2,zoompan=z='if(lte(zoom,1.0),1.5,max(1.001,zoom-0.00{speed}))':d={duration * 50}:x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':fps=50\" " +
    //                                          $"-preset veryslow -crf 30 \"{videoOutputPath}\"";
    //                     break;
    //             }






    //             //string ffmpegCommand = $"-y -loop 1 -i \"{inputPath}\" -vf \"scale=3840:-2,zoompan=z='if(lte(zoom,1.0),1.5,max(1.001,zoom-0.00{speed}))':d={duration * 50}:x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':fps=50\" -c:v libx264 -t {duration} -crf 30 \"{videoOutputPath}\"";

    //             await Console.Out.WriteLineAsync("following command <begin>" + ffmpegCommand + "<end>");

    //             string result = await _videoProcessingService.ExecuteFfmpegCommandAsync(ffmpegCommand);


    //             if (result != "")
    //             {
    //                 return result;
    //             }
    //             //ProcessStartInfo startInfo = new ProcessStartInfo(_secretsConfiguration.FFMPEGExecutable_path)
    //             //{
    //             //    Arguments = ffmpegCommand,
    //             //    RedirectStandardOutput = true,
    //             //    RedirectStandardError = true,
    //             //    UseShellExecute = false,
    //             //    CreateNoWindow = true
    //             //};

    //             //using (Process process = new Process { StartInfo = startInfo })
    //             //{
    //             //    process.Start();
    //             //    process.BeginOutputReadLine();
    //             //    process.BeginErrorReadLine();
    //             //    await process.WaitForExitAsync();
    //             //}

    //             //string videBlobName = $"clip-{Guid.NewGuid()}.mp4";
    //             //Uri uri = await _azurestorageservice.UploadVideoToBlobAsyncInChunks(videoOutputPath, videBlobName, "finalfiles");
    //             //if (uri != null)
    //             //{
    //             //    string sas = await _azurestorageservice.GenerateBlobSasTokenAsync(uri);
    //             //    await Console.Out.WriteLineAsync("uri is null ");
    //             //    string RealOutput = $"{uri}{sas}";
    //             //    // Return the cloud path of the generated video file if successful
    //             //    return RealOutput;

    //             //}
    //             attempt++;


    //         }
    //         catch (Exception ex)
    //         {
    //             Debug.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
    //             attempt++;

    //             if (attempt >= maxAttempts)
    //             {
    //                 Debug.WriteLine("Maximum retry attempts reached, failing operation.");
    //                 return null; // Or throw the last exception
    //             }
    //         }
    //     }

    //     return null; // This should not be reached, but it's here to satisfy the compiler.
    // }


    public async Task<string> GenerateVideoFromImageAndStoreLocally(string ffmpegCommand)
    {
        int maxAttempts = 3;
        int attempt = 0;

        while (attempt < maxAttempts)
        {
            try

            {

        
     

                string videoOutputPath = Path.GetTempFileName();
                videoOutputPath = Path.ChangeExtension(videoOutputPath, ".mp4");

                // Append the output path to the ffmpeg command here, making sure to enclose it in quotes
                ffmpegCommand += $" \"{videoOutputPath}\"";

                if (File.Exists(videoOutputPath))
                {
                    File.Delete(videoOutputPath);
                }

                Random rand = new Random();
                int num = rand.Next(0, 4);



                ProcessStartInfo startInfo = new ProcessStartInfo(_secretsConfiguration.FFMPEGExecutable_path)
                {
                    Arguments = ffmpegCommand,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();
                }


                if (File.Exists(videoOutputPath))
                {
                    string videoName = $"clip-{Guid.NewGuid()}.mp4";

                    Uri newUri = await _azurestorageservice.UploadVideoToBlobAsyncInChunks(videoOutputPath, videoName, "finalfiles");
                    string sas = await _azurestorageservice.GenerateBlobSasTokenAsync(newUri);

                    string final = $"{newUri}{sas}";

                    // Already checked wether file exists
                    File.Delete(videoOutputPath);

                    return final;
                }

                //string videBlobName = $"clip-{Guid.NewGuid()}.mp4";
                //Uri uri = await _azurestorageservice.UploadVideoToBlobAsyncInChunks(videoOutputPath, videBlobName, "finalfiles");
                //if (uri != null)
                //{
                //    string sas = await _azurestorageservice.GenerateBlobSasTokenAsync(uri);
                //    await Console.Out.WriteLineAsync("uri is null ");
                //    string RealOutput = $"{uri}{sas}";
                //    // Return the cloud path of the generated video file if successful
                //    return RealOutput;

                //}
                attempt++;


            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
                attempt++;

                if (attempt >= maxAttempts)
                {
                    Debug.WriteLine("Maximum retry attempts reached, failing operation.");
                    return null; // Or throw the last exception
                }
            }
        }

        return null; // This should not be reached, but it's here to satisfy the compiler.
    }


    // LOCALLY 

    public async Task<string> JoinVideosAndStoreLocallyAsyncREALLYLOCAL(List<string> videoPaths)
    {
        // First, check if videoPaths is null or has no elements
        if (videoPaths == null || videoPaths.Count == 0)
        {
            Console.WriteLine("video paths have 0 elemeents in them on input");
            return null;
        }

        // Remove all null elements and elements not containing a SAS token from the list
        videoPaths.RemoveAll(item => item == null);

        // After cleaning, check again if the list has elements
        if (videoPaths.Count == 0)
        {
            // If the list is empty after removing, return null
            Console.WriteLine("video paths have 0 elemeents in them after filter");
            return null;
        }

        try
        {
            // Join the videos. Ensure this method is designed to work with the paths of the downloaded videos
            string joinedVideoPath = await JoinVideosAsynclocally(videoPaths);



            return joinedVideoPath;
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync("An error occurred while joining videos: " + ex.Message);
            return null;
        }
    }


    public async Task<string> JoinVideosAsynclocally(List<string> videoPaths)
    {
        if (videoPaths == null || videoPaths.Count == 0)
        {
            return "An error occurred, Video paths are empty";
        }

        string outputPath = Path.GetTempFileName();
        outputPath = Path.ChangeExtension(outputPath, ".mp4");

        // Construct the FFmpeg command for joining videos
        var inputArgs = string.Join(" ", videoPaths.Select((path, index) => $"-i \"{path}\""));
        var filterArgs = string.Join("", videoPaths.Select((_, index) => $"[{index}:v]"));
        var ffmpegCommand = $"{inputArgs} -filter_complex \"{filterArgs}concat=n={videoPaths.Count}:v=1:a=0[outv]\" -map \"[outv]\" \"{outputPath}\"";

        await Console.Out.WriteLineAsync($"ffmpeg command = " + ffmpegCommand);

        ProcessStartInfo startInfo = new ProcessStartInfo(_secretsConfiguration.FFMPEGExecutable_path)
        {
            Arguments = ffmpegCommand,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();
            }

            // Assuming _azurestorageservice is your Azure storage service for uploading and generating SAS


            Console.WriteLine("joined video is here => " + outputPath);


            return outputPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred: {ex.Message}");
            return null; // Consider handling the error more gracefully
        }
    }












    // IN THE CLOUD
    public async Task<string> JoinVideoStoreInCloud(List<string> videoPaths)
    {
        // First, check if videoPaths is null or has no elements
        if (videoPaths == null || videoPaths.Count == 0)
        {
            Console.WriteLine("video paths have 0 elemeents in them on input");
            return null;
        }

        // Remove all null elements and elements not containing a SAS token from the list
        videoPaths.RemoveAll(item => item == null);

        // After cleaning, check again if the list has elements
        if (videoPaths.Count == 0)
        {
            // If the list is empty after removing, return null
            Console.WriteLine("video paths have 0 elemeents in them after filter");
            return null;
        }

        try
        {
            // Join the videos. Ensure this method is designed to work with the paths of the downloaded videos
            string joinedVideoPath = await JoinVideosAsync(videoPaths);

            return joinedVideoPath;
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync("An error occurred while joining videos: " + ex.Message);
            return null;
        }
    }




    public async Task<string> JoinVideosAsync(List<string> videoPaths)
    {
        if (videoPaths == null || videoPaths.Count == 0)
        {
            return "An error occurred, video paths are empty";
        }

        // Create a temporary file to list all video files for FFmpeg
        string fileListPath = Path.GetTempFileName();
        using (var fileStream = new StreamWriter(fileListPath))
        {
            foreach (var path in videoPaths)
            {
                // Write the file path in the format required by FFmpeg's concat demuxer
                await fileStream.WriteLineAsync($"file '{path}'");
            }
        }

        string outputPath = Path.GetTempFileName();
        outputPath = Path.ChangeExtension(outputPath, ".mp4");

        int maxRetries = 5;
        int attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                // Construct the FFmpeg command for joining videos using the concat demuxer
                var ffmpegCommand = $"-f concat -safe 0 -i \"{fileListPath}\" -c copy \"{outputPath}\"";

                ProcessStartInfo startInfo = new ProcessStartInfo(_secretsConfiguration.FFMPEGExecutable_path)
                {
                    Arguments = ffmpegCommand,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    // Since we're now using the concat demuxer, there's no need to begin reading output/error lines
                    await process.WaitForExitAsync();
                }

                if (!File.Exists(outputPath))
                {
                    throw new FileNotFoundException($"Output file was not created: {outputPath}");
                }
                else
                {
                    string clipname = $"clip-{Guid.NewGuid()}.mp4";
                    Uri clipUri = await _azurestorageservice.UploadVideoToBlobAsync(outputPath, clipname, "finalfiles");
                    string sas = await _azurestorageservice.GenerateBlobSasTokenAsync(clipUri);
                    string final_Cloud = $"{clipUri}{sas}";

                    File.Delete(outputPath); // Clean up the temporary output file
                    File.Delete(fileListPath); // Clean up the temporary file list

                    return final_Cloud;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
                if (attempt == maxRetries - 1)
                {
                    return $"Failed to join videos after {maxRetries} attempts. Last error: {ex.Message}";
                }
                await Task.Delay(1000 * (attempt + 1));
                attempt++;
            }
        }

        // Clean up in case of failure to prevent leftover temporary files
        File.Delete(fileListPath);

        return null; // Consider a more explicit error handling or fallback mechanism here
    }







    public async Task<string> ReplaceAudioInVideo(string videoPath, string audioPath)
    {
        if (string.IsNullOrEmpty(videoPath) || string.IsNullOrEmpty(audioPath))
        {
            throw new ArgumentException("Video path and audio path cannot be null or empty.");
        }

        string outputPath = Path.ChangeExtension(Path.GetTempFileName(), ".mp4");
        int maxRetries = 5;
        StringBuilder outputLog = new StringBuilder();
        StringBuilder errorLog = new StringBuilder();

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                string ffmpegCommand = $"-i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -map 0:v:0 -map 1:a:0 \"{outputPath}\"";
                ProcessStartInfo startInfo = new ProcessStartInfo(_secretsConfiguration.FFMPEGExecutable_path)
                {
                    Arguments = ffmpegCommand,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.OutputDataReceived += (sender, args) => outputLog.AppendLine(args.Data);
                    process.ErrorDataReceived += (sender, args) => errorLog.AppendLine(args.Data);

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0 && File.Exists(outputPath))
                    {
                        Console.WriteLine("Output file created successfully: " + outputPath);

                        string clipname = $"video-{Guid.NewGuid()}.mp4";

                        Uri clipUri = await _azurestorageservice.UploadVideoToBlobAsync(outputPath, clipname, "finalfiles");
                        string sas = await _azurestorageservice.GenerateBlobSasTokenAsync(clipUri);

                        string final_Cloud = $"{clipUri}{sas}";


                        File.Delete(outputPath);

                        return final_Cloud;

                
                    }
                    else
                    {
                        throw new InvalidOperationException("FFmpeg process failed or output file was not created.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
                Console.WriteLine($"FFmpeg Output: {outputLog}");
                Console.WriteLine($"FFmpeg Errors: {errorLog}");
                outputLog.Clear(); // Clear the logs for the next attempt
                errorLog.Clear();

                if (attempt == maxRetries - 1)
                {
                    // This was the last attempt
                    throw new InvalidOperationException($"Failed to replace audio in video after {maxRetries} attempts.", ex);
                }

                // Wait for a linearly increasing amount of time before retrying
                await Task.Delay(1000 * (attempt + 1));
            }
        }

        // Ideally, this point should never be reached due to the throw in the last attempt
        return null;
    }





  


  

   
}