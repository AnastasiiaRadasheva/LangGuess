using LangGuess.Services;

namespace LangGuess;

public partial class App : Application
{
    // UseMauiApp<App>() регистрирует App в DI → SettingsService инжектируется автоматически
    public App(SettingsService settings)
    {
        InitializeComponent();
        UserAppTheme = settings.IsDark ? AppTheme.Dark : AppTheme.Light;
        LocalizationService.Instance.SetLanguage(settings.Language);
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell());
}
