using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Snakes.BlazorUI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<GameJsInterop>();
await builder.Build().RunAsync();
