using Microsoft.AspNetCore.ResponseCompression;
using Orleans.Configuration;
using Orleans;
using Snakes;
using Microsoft.AspNetCore.SignalR;

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
        .UseLocalhostClustering()
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "dev";
            options.ServiceId = "Snakes";
        })
        .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning).AddConsole())
        .Build();

    await client.Connect();
    Console.WriteLine("Client successfully connected to silo host \n");
    return client;
}
