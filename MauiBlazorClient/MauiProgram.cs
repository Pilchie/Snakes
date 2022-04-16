using Microsoft.AspNetCore.Components.WebView.Maui;
using Snakes.MauiBlazorClient.Data;

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

        builder.Services.AddSingleton<WeatherForecastService>();

        return builder.Build();
    }
}
