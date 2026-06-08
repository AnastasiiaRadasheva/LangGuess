using LangGuess.Services;
using LangGuess.ViewModels;
using LangGuess.Views;
using Microsoft.Extensions.Logging;

namespace LangGuess;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf",  "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services (AudioService accesses AudioManager.Current lazily — no DI for IAudioManager)
        builder.Services.AddSingleton(LocalizationService.Instance);
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<GameService>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<AudioService>();

        // ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<GameViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<StreakViewModel>();

        // Views
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<GamePage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<StreakPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
