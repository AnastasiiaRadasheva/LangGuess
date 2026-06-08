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
        // Small delay so Android audio system is ready before we create a MediaPlayer
        await Task.Delay(300);
        await _audio.PreloadAsync();
        await _audio.StartBackgroundMusicAsync();
    }
}
