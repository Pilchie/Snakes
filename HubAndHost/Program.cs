using Microsoft.AspNetCore.ResponseCompression;
using Orleans.Configuration;
using Orleans;
using Snakes;
using Microsoft.AspNetCore.SignalR;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts
    => opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" }));

var client = await ConnectClient();
builder.Services.AddSingleton(client);
var app = builder.Build();

app.UseResponseCompression();

app.MapGet("/", () => "Hello World!");
app.MapHub<SnakeHub>("/snakehub");
app.Run();

static async Task<IClusterClient> ConnectClient()
{
    IClusterClient client;
    client = new ClientBuilder()
        .UseStaticClustering(
            new IPEndPoint(IPAddress.Loopback, 30000)
        )
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "dev";
            options.ServiceId = "Snakes";
        })
        .ConfigureLogging(logging => logging/*.SetMinimumLevel(LogLevel.Warning)*/.AddConsole())
        .Build();

    Console.WriteLine("Client about to connect to silo host \n");
    await client.Connect();
    Console.WriteLine("Client successfully connected to silo host \n");
    return client;
}
