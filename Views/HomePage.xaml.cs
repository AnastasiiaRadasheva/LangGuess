using LangGuess.Services;
using LangGuess.ViewModels;

namespace LangGuess.Views;

public partial class HomePage : ContentPage
{
    public HomePage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not HomeViewModel)
            BindingContext = IPlatformApplication.Current!.Services
                                .GetRequiredService<HomeViewModel>();

        // Pre-load SFX files into cache so taps are instant.
        // Music is managed by App.OnResume / App.OnSleep — not by pages.
        var audio = IPlatformApplication.Current!.Services
                        .GetRequiredService<AudioService>();
        await audio.PreloadAsync();
    }
}
