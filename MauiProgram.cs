using LangGuess.Services;
using LangGuess.ViewModels;
using LangGuess.Views;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;

namespace LangGuess;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()  // <-- уже регистрирует App в DI, AddSingleton<App>() НЕ нужен
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf",  "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton(LocalizationService.Instance);
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<GameService>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<AudioService>();

        // IAudioManager: lazy factory — безопасно на Android
        builder.Services.AddSingleton<IAudioManager>(_ =>
        {
            try { return AudioManager.Current; }
            catch { return null!; }
        });

        // ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<GameViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Views
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<GamePage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
