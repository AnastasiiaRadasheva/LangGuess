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

        // Start background music here — most reliable lifecycle point on Android
        var audio = IPlatformApplication.Current!.Services
                        .GetRequiredService<AudioService>();
        await audio.StartBackgroundMusicAsync();
    }
}
