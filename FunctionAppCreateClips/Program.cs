
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;




var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Add Application Insights telemetry services
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddHttpClient();

        // Add your custom scoped services here
        services.AddScoped<IAzureStorageService, AzureStorageService>();
        services.AddScoped<IJwtTokenServices, JwtTokenServices>();
        services.AddScoped<IVideoCreationServices, VideoCreationServices>();
        services.AddSingleton<ISecretsConfiguration, SecretsConfiguration>();


        services.AddHttpClient("MyCustomHttpClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(600);
        });
    })
    .Build();




var secretsConfig = host.Services.GetRequiredService<ISecretsConfiguration>();
await secretsConfig.LoadSecretsAsync();

secretsConfig.FFMPEGExecutable_path = "C:\\home\\site\\wwwroot\\Xtra_Dependencies\\FFMPEG\\ffmpeg.exe";



host.Run();
