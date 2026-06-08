using LangGuess.Services;
using LangGuess.ViewModels;

namespace LangGuess.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not SettingsViewModel)
            BindingContext = IPlatformApplication.Current!.Services
                                .GetRequiredService<SettingsViewModel>();

        if (BindingContext is SettingsViewModel vm)
            await vm.LoadHistoryAsync();

        // Safety net: restart music if something stopped it during navigation
        var audio = IPlatformApplication.Current!.Services.GetRequiredService<AudioService>();
        await audio.StartBackgroundMusicAsync();
    }
}
