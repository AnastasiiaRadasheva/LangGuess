using LangGuess.Services;

namespace LangGuess;

public partial class App : Application
{
    private readonly AudioService _audio;

    public App(SettingsService settings, AudioService audio)
    {
        InitializeComponent();
        _audio = audio;
        UserAppTheme = settings.IsDark ? AppTheme.Dark : AppTheme.Light;
        LocalizationService.Instance.SetLanguage(settings.Language);
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell());

    protected override void OnSleep()
    {
        base.OnSleep();
        _audio.StopBackgroundMusic();
    }

    protected override async void OnResume()
    {
        base.OnResume();
        await _audio.StartBackgroundMusicAsync();
    }
}
