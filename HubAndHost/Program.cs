using Microsoft.AspNetCore.ResponseCompression;
using Orleans.Configuration;
using Orleans;
using Snakes;
using Orleans.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

var secretClient = new SecretClient(new Uri("https://sneks-kv.vault.azure.net/"), new DefaultAzureCredential());
var storageConnectionString = await Utilities.GetStorageConnectionString();
builder.Services.AddAzureClientsCore();
builder.Services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(storageConnectionString, "data-protection-container", "data-protection-blob")
    .ProtectKeysWithAzureKeyVault(new Uri(Utilities.AzureKeyvaultUrl), new DefaultAzureCredential());

builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts
    => opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" }));

var client = await ConnectClient(storageConnectionString);
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

static async Task<IClusterClient> ConnectClient(string storageConnectionString)
{
    var builder = new ClientBuilder()
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = ClusterInfo.ClusterId;
            options.ServiceId = "Snakes";
        })
        .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning).AddJsonConsole())
        .UseAzureStorageClustering(options => options.ConfigureTableServiceClient(storageConnectionString));

    Console.WriteLine("Client about to connect to silo host \n");
    var client = builder.Build();
    await client.Connect();
    Console.WriteLine("Client successfully connected to silo host \n");
    return client;
}
