using LangGuess.Services;
using LangGuess.ViewModels;

namespace LangGuess.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not HomeViewModel)
            BindingContext = IPlatformApplication.Current!.Services
                                .GetRequiredService<HomeViewModel>();

        // Pre-load SFX bytes + start background music (most reliable Android lifecycle point)
        var audio = IPlatformApplication.Current!.Services
                        .GetRequiredService<AudioService>();
        // Run both in parallel: preload SFX bytes into RAM, start background track
        await Task.WhenAll(
            audio.PreloadAsync(),
            audio.StartBackgroundMusicAsync()
        );

#if DEBUG
        // Show audio diagnostics on screen so we can see what's failing on device
        AudioDiagLabel.Text      = audio.LastError;
        AudioDiagLabel.IsVisible = !string.IsNullOrEmpty(audio.LastError);
#endif
    }
}
