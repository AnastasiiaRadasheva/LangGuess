using LangGuess.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LangGuess.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Резолвим ViewModel из DI если Shell создал страницу без него
        if (BindingContext is not SettingsViewModel)
            BindingContext = IPlatformApplication.Current!.Services
                                .GetRequiredService<SettingsViewModel>();

        if (BindingContext is SettingsViewModel vm)
            await vm.LoadHistoryAsync();
    }
}
