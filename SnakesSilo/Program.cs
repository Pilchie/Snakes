using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Snakes;
using System.Net;

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
    var addresses = await Dns.GetHostAddressesAsync("snakessilo");
    var primarySiloEndpoint = new IPEndPoint(addresses.First(), 11111);
    // define the cluster configuration
    var builder = new SiloHostBuilder()
        .UseDevelopmentClustering(primarySiloEndpoint)
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "dev";
            options.ServiceId = "Snakes";
        })
        .ConfigureEndpoints(siloPort: 11_111, gatewayPort: 30_000)
        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(PlayerGrain).Assembly).WithReferences())
        .ConfigureLogging(logging => logging/*.SetMinimumLevel(LogLevel.Warning)*/.AddConsole());

    var host = builder.Build();
    await host.StartAsync();
    return host;
}
