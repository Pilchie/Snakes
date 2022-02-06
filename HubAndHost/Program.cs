using Microsoft.AspNetCore.ResponseCompression;
using Orleans.Configuration;
using Orleans;
using Snakes;
using Microsoft.AspNetCore.SignalR;
using System.Net;
using Orleans.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts
    => opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" }));

var client = await ConnectClient();
builder.Services.AddSingleton(client);
var app = builder.Build();

app.UseResponseCompression();

app.UseStaticFiles();

app.MapGet("/", () => "Hello World!");
app.MapHub<SnakeHub>("/snakehub");
app.Run();

static async Task<IClusterClient> ConnectClient()
{
    var builder = new ClientBuilder()
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = GitInfo.GitSha;
            options.ServiceId = "Snakes";
        })
        .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning).AddJsonConsole());

    const bool local = false;
    if (local)
    {
        var addresses = await Dns.GetHostAddressesAsync("snakessilo");
        builder.UseStaticClustering(
            addresses.Select(a => new IPEndPoint(a, 30_000)).ToArray());
    }
    else
    {
        var connectionString = Utilities.GetStorageConnectionString();
        builder.UseAzureStorageClustering(options => options.ConfigureTableServiceClient(connectionString));
    }

    Console.WriteLine("Client about to connect to silo host \n");
    var client = builder.Build();
    await client.Connect();
    Console.WriteLine("Client successfully connected to silo host \n");
    return client;
}
