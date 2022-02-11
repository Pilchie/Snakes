using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Snakes;
using System.Net;
using System.Runtime;

try
{
    var host = await StartSilo();
    //Console.WriteLine("\n\n Press Enter to terminate...\n\n");
    //Console.ReadLine();

    Console.CancelKeyPress += (s,e) => host.StopAsync(); 
    while (true)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return 1;
}

static async Task<ISiloHost> StartSilo()
{
    var builder = new SiloHostBuilder()
        .Configure<ClusterOptions>(options =>
{
            options.ClusterId = ClusterInfo.ClusterId;
            options.ServiceId = "Snakes";
        })
        .ConfigureEndpoints(siloPort: 11_111, gatewayPort: 30_000)
        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(PlayerGrain).Assembly).WithReferences())
        .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning).AddJsonConsole())
        .UseAzureStorageClustering(options => options.ConfigureTableServiceClient(Utilities.GetStorageConnectionString()));

    var host = builder.Build();
    await host.StartAsync();
    return host;
}
