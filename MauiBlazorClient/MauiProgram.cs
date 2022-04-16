using Microsoft.AspNetCore.Components.WebView.Maui;
using Snakes.BlazorUI;

namespace Snakes.MauiBlazorClient;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://sneks-hub.blueisland-9b5cb61a.eastus2.azurecontainerapps.io") });
        builder.Services.AddScoped<GameJsInterop>();

        return builder.Build();
    }
}
