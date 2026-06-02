using LangGuess.Models;
using LangGuess.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace LangGuess.Views;

public partial class GamePage : ContentPage
{
    private GameViewModel _vm = null!;

    // Pixels the card must be swiped upward to trigger a guess
    private const double SwipeThreshold = 60;

    public GamePage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not GameViewModel)
            BindingContext = IPlatformApplication.Current!.Services
                                .GetRequiredService<GameViewModel>();

        _vm = (GameViewModel)BindingContext;
        _vm.AvailableLanguages.CollectionChanged += (_, _) => RefreshLangCards();
        await _vm.InitAsync();
        RefreshLangCards();

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
            StrokeShape     = new RoundRectangle { CornerRadius = 16 },
            Padding         = new Thickness(8, 8),
            Content         = new VerticalStackLayout
            {
                Spacing           = 3,
                VerticalOptions   = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children          = { abbr, name, ext }
            }
        };

        // Tap = instant guess
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => MakeGuess(lang);
        card.GestureRecognizers.Add(tap);

        // 4-stage swipe-up gesture
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
    //  4-STAGE SWIPE-UP GESTURE  (PanGestureRecognizer)
    //
    //  Stage 1 – Started  : finger touches card → scale up, highlight border
    //  Stage 2 – Running  : finger moves up → card follows, border turns green
    //                       when past threshold
    //  Stage 3 – Completed: released past threshold → fly up + guess
    //                       released below threshold  → spring back
    //  Stage 4 – Canceled : gesture interrupted → spring back immediately
    // ─────────────────────────────────────────────────────────────────────────
    private void AttachSwipeGesture(Border card, ProgrammingLanguage lang, bool isDark)
    {
        var colorNormal = new SolidColorBrush(
            isDark ? Color.FromArgb("#5E3BA8") : Color.FromArgb("#7C3AED"));
        var colorActive = new SolidColorBrush(Color.FromArgb("#A374FF"));
        var colorReady  = new SolidColorBrush(Color.FromArgb("#2EA043")); // green = release to guess

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += async (_, e) =>
        {
            switch (e.StatusType)
            {
                // ── Stage 1: Started ──────────────────────────────────────────
                case GestureStatus.Started:
                    card.Stroke          = colorActive;
                    card.StrokeThickness = 2.5;
                    await card.ScaleToAsync(1.08, 80, Easing.CubicOut);
                    break;

                // ── Stage 2: Running ──────────────────────────────────────────
                case GestureStatus.Running:
                    // Allow only upward movement (clamp to 0)
                    card.TranslationY = Math.Min(0, e.TotalY);
                    // Border turns green when the card has passed the threshold
                    card.Stroke = card.TranslationY < -SwipeThreshold
                        ? colorReady
                        : colorActive;
                    break;

                // ── Stage 3: Completed ────────────────────────────────────────
                case GestureStatus.Completed:
                    if (card.TranslationY < -SwipeThreshold)
                    {
                        // Swiped far enough → fly card off-screen and guess
                        await Task.WhenAll(
                            card.TranslateToAsync(0, -400, 220, Easing.CubicIn),
                            card.FadeToAsync(0, 200));
                        MakeGuess(lang);
                    }
                    else
                    {
                        // Not far enough → spring back to original position
                        await Task.WhenAll(
                            card.TranslateToAsync(0, 0, 280, Easing.SpringOut),
                            card.ScaleToAsync(1.0, 220));
                        card.Stroke          = colorNormal;
                        card.StrokeThickness = 1.5;
                    }
                    break;

                // ── Stage 4: Canceled ─────────────────────────────────────────
                case GestureStatus.Canceled:
                    // Gesture interrupted → snap back immediately
                    await Task.WhenAll(
                        card.TranslateToAsync(0, 0, 200, Easing.SpringOut),
                        card.ScaleToAsync(1.0, 180));
                    card.Stroke          = colorNormal;
                    card.StrokeThickness = 1.5;
                    break;
            }
        };
        card.GestureRecognizers.Add(pan);
    }
}
