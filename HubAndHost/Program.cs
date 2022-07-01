using Microsoft.AspNetCore.ResponseCompression;
using Orleans.Configuration;
using Orleans;
using Snakes;
using Microsoft.AspNetCore.SignalR;
using System.Net;
using Orleans.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Azure.Identity;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddAzureClientsCore();
    builder.Services.AddDataProtection()
        .PersistKeysToAzureBlobStorage(Utilities.GetStorageConnectionString(), "data-protection-container", "data-protection-blob")
        .ProtectKeysWithAzureKeyVault(new Uri("https://sneks-kv.vault.azure.net/keys/dataprotection-key"), new DefaultAzureCredential());
}

builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts
    => opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" }));

var client = await ConnectClient();
builder.Services.AddSingleton(client);
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseResponseCompression();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapHub<SnakeHub>("/snakehub");

app.Run();

static async Task<IClusterClient> ConnectClient()
{
    var builder = new ClientBuilder()
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = ClusterInfo.ClusterId;
            options.ServiceId = "Snakes";
        })
        .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning).AddJsonConsole())
        .UseAzureStorageClustering(options => options.ConfigureTableServiceClient(Utilities.GetStorageConnectionString()));

    Console.WriteLine("Client about to connect to silo host \n");
    var client = builder.Build();
    await client.Connect();
    Console.WriteLine("Client successfully connected to silo host \n");
    return client;
}
