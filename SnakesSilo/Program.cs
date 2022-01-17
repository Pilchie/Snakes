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
    // define the cluster configuration
    var builder = new SiloHostBuilder()
        .UseDevelopmentClustering(new IPEndPoint(IPAddress.Loopback, 11111))
        .ConfigureEndpoints(IPAddress.Loopback, siloPort: 11111, gatewayPort: 30000, listenOnAnyHostAddress: true)
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "dev";
            options.ServiceId = "Snakes";
        })
        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(PlayerGrain).Assembly).WithReferences())
        .ConfigureLogging(logging => logging/*.SetMinimumLevel(LogLevel.Warning)*/.AddConsole());

    var host = builder.Build();
    await host.StartAsync();
    return host;
}
