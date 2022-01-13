using HubAndHost.Hubs;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});
var app = builder.Build();

app.UseResponseCompression();
app.MapGet("/", () => "Hello World!");
app.MapHub<SnakeHub>("/snakehub");
app.Run();
