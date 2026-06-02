using LangGuess.ViewModels;

namespace LangGuess.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not HomeViewModel)
            BindingContext = IPlatformApplication.Current!.Services
                                .GetRequiredService<HomeViewModel>();
    }
}
