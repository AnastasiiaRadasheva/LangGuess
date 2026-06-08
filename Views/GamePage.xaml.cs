using LangGuess.Models;
using LangGuess.Services;
using LangGuess.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace LangGuess.Views;

public partial class GamePage : ContentPage
{
    private GameViewModel _vm    = null!;
    private AudioService  _audio = null!;
    private bool _drawerOpen = false;

    private const double SwipeThreshold = 60;

    public GamePage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not GameViewModel)
            BindingContext = IPlatformApplication.Current!.Services
                                .GetRequiredService<GameViewModel>();

        _vm    = (GameViewModel)BindingContext;
        _audio = IPlatformApplication.Current!.Services.GetRequiredService<AudioService>();
        _vm.AvailableLanguages.CollectionChanged += (_, _) => RefreshLangCards();
        await _vm.InitAsync();
        RefreshLangCards();
        BuildDrawerList();

        RowsScroll.Scrolled += (_, e) =>
            HeaderScroll.ScrollToAsync(e.ScrollX, 0, false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  LANGUAGE CARDS
    // ─────────────────────────────────────────────────────────────────────────
    private void RefreshLangCards()
    {
        LangLayout.Children.Clear();
        foreach (var lang in _vm.AvailableLanguages)
            LangLayout.Add(BuildLangCard(lang));
    }

    private Border BuildLangCard(ProgrammingLanguage lang)
    {
        bool isDark = Application.Current?.UserAppTheme != AppTheme.Light;

        var abbr = new Label
        {
            Text              = lang.Abbr,
            FontSize          = 20,
            FontAttributes    = FontAttributes.Bold,
            TextColor         = isDark ? Color.FromArgb("#A374FF") : Color.FromArgb("#6D28D9"),
            HorizontalOptions = LayoutOptions.Center
        };
        var name = new Label
        {
            Text              = lang.Name,
            FontSize          = 11,
            TextColor         = isDark ? Color.FromArgb("#9B8EC4") : Color.FromArgb("#5C4F7C"),
            HorizontalOptions = LayoutOptions.Center,
            MaxLines          = 1,
            LineBreakMode     = LineBreakMode.TailTruncation
        };
        var ext = new Label
        {
            Text              = lang.FileExt,
            FontSize          = 10,
            TextColor         = isDark ? Color.FromArgb("#5C5080") : Color.FromArgb("#8B7DC5"),
            HorizontalOptions = LayoutOptions.Center
        };

        var card = new Border
        {
            WidthRequest    = 96,
            HeightRequest   = 100,
            BackgroundColor = isDark ? Color.FromArgb("#1A1435") : Color.FromArgb("#E3DBFF"),
            Stroke          = new SolidColorBrush(isDark ? Color.FromArgb("#5E3BA8") : Color.FromArgb("#7C3AED")),
            StrokeThickness = 1.5,
            StrokeShape     = new RoundRectangle { CornerRadius = 8 },
            Padding         = new Thickness(8, 8),
            Content         = new VerticalStackLayout
            {
                Spacing           = 3,
                VerticalOptions   = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children          = { abbr, name, ext }
            }
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => MakeGuess(lang);
        card.GestureRecognizers.Add(tap);
        AttachSwipeGesture(card, lang, isDark);

        return card;
    }

    private void MakeGuess(ProgrammingLanguage lang)
    {
        if (_vm.IsGameOver) return;
        _vm.GuessCommand.Execute(lang);
        RefreshLangCards();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INFO DRAWER
    // ─────────────────────────────────────────────────────────────────────────
    private void BuildDrawerList()
    {
        DrawerLangList.Children.Clear();
        bool isDark = Application.Current?.UserAppTheme != AppTheme.Light;

        foreach (var lang in _vm.AllLanguages.OrderBy(l => l.Name))
        {
            var item = new Grid
            {
                Padding            = new Thickness(14, 12),
                ColumnDefinitions  = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }
            };

            var abbr = new Border
            {
                WidthRequest    = 44,
                HeightRequest   = 44,
                BackgroundColor = isDark ? Color.FromArgb("#1A1435") : Color.FromArgb("#DDD6FF"),
                Stroke          = new SolidColorBrush(isDark ? Color.FromArgb("#5E3BA8") : Color.FromArgb("#7C3AED")),
                StrokeThickness = 1,
                StrokeShape     = new RoundRectangle { CornerRadius = 6 },
                Margin          = new Thickness(0, 0, 12, 0),
                Content         = new Label
                {
                    Text              = lang.Abbr,
                    FontSize          = 13,
                    FontAttributes    = FontAttributes.Bold,
                    TextColor         = isDark ? Color.FromArgb("#A374FF") : Color.FromArgb("#6D28D9"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions   = LayoutOptions.Center
                }
            };

            var nameLabel = new Label
            {
                Text           = $"{lang.Name}  ({lang.Year})",
                FontSize       = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor      = isDark ? Color.FromArgb("#E2D9F3") : Color.FromArgb("#1A1035")
            };
            var descLabel = new Label
            {
                Text      = lang.Description,
                FontSize  = 11,
                TextColor = isDark ? Color.FromArgb("#7B6FA8") : Color.FromArgb("#5C4F7C"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            var textStack = new VerticalStackLayout { Spacing = 3, Children = { nameLabel, descLabel } };

            Grid.SetColumn(abbr, 0);
            Grid.SetColumn(textStack, 1);
            item.Children.Add(abbr);
            item.Children.Add(textStack);

            // Separator
            var sep = new BoxView
            {
                HeightRequest   = 1,
                BackgroundColor = isDark ? Color.FromArgb("#1E1640") : Color.FromArgb("#C4B5FD"),
                Margin          = new Thickness(14, 0)
            };

            DrawerLangList.Children.Add(item);
            DrawerLangList.Children.Add(sep);
        }
    }

    private async void OnDrawerToggleClicked(object? sender, EventArgs e)
    {
        _audio.PlayTap();
        if (_drawerOpen) await CloseDrawerAsync();
        else             await OpenDrawerAsync();
    }

    private void OnDrawerCloseClicked(object? sender, EventArgs e)
        => _ = CloseDrawerAsync();

    private void OnDimTapped(object? sender, TappedEventArgs e)
        => _ = CloseDrawerAsync();

    private async Task OpenDrawerAsync()
    {
        _drawerOpen    = true;
        DrawerDim.IsVisible = true;
        await Task.WhenAll(
            DrawerPanel.TranslateToAsync(0, 0, 250, Easing.CubicOut),
            DrawerDim.FadeToAsync(0.45, 250));
    }

    private async Task CloseDrawerAsync()
    {
        _drawerOpen = false;
        await Task.WhenAll(
            DrawerPanel.TranslateToAsync(-290, 0, 220, Easing.CubicIn),
            DrawerDim.FadeToAsync(0, 220));
        DrawerDim.IsVisible = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  4-STAGE SWIPE-UP GESTURE
    // ─────────────────────────────────────────────────────────────────────────
    private void AttachSwipeGesture(Border card, ProgrammingLanguage lang, bool isDark)
    {
        var colorNormal = new SolidColorBrush(isDark ? Color.FromArgb("#5E3BA8") : Color.FromArgb("#7C3AED"));
        var colorActive = new SolidColorBrush(Color.FromArgb("#A374FF"));
        var colorReady  = new SolidColorBrush(Color.FromArgb("#2EA043"));

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += async (_, e) =>
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    card.Stroke = colorActive; card.StrokeThickness = 2.5;
                    await card.ScaleToAsync(1.08, 80, Easing.CubicOut);
                    break;
                case GestureStatus.Running:
                    card.TranslationY = Math.Min(0, e.TotalY);
                    card.Stroke = card.TranslationY < -SwipeThreshold ? colorReady : colorActive;
                    break;
                case GestureStatus.Completed:
                    if (card.TranslationY < -SwipeThreshold)
                    {
                        await Task.WhenAll(
                            card.TranslateToAsync(0, -400, 220, Easing.CubicIn),
                            card.FadeToAsync(0.0, 200));
                        MakeGuess(lang);
                    }
                    else
                    {
                        await Task.WhenAll(card.TranslateToAsync(0, 0, 280, Easing.SpringOut), card.ScaleToAsync(1.0, 220));
                        card.Stroke = colorNormal; card.StrokeThickness = 1.5;
                    }
                    break;
                case GestureStatus.Canceled:
                    await Task.WhenAll(card.TranslateToAsync(0, 0, 200, Easing.SpringOut), card.ScaleToAsync(1.0, 180));
                    card.Stroke = colorNormal; card.StrokeThickness = 1.5;
                    break;
            }
        };
        card.GestureRecognizers.Add(pan);
    }
}
