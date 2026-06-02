using LangGuess.Models;
using LangGuess.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace LangGuess.Views;

public partial class GamePage : ContentPage
{
    private GameViewModel _vm = null!;

    // ── Drag state ────────────────────────────────────────────────────────────
    private Border? _ghostView;
    private ProgrammingLanguage? _draggedLang;
    private bool _isDragging;
    private Point _ghostStartPos;

    public GamePage()
    {
        InitializeComponent();
    }

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

        // Sync header scroll with rows scroll
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
        {
            var card = BuildLangCard(lang);
            LangLayout.Add(card);
        }
    }

    private Border BuildLangCard(ProgrammingLanguage lang)
    {
        bool isDark = Application.Current?.UserAppTheme != AppTheme.Light;

        var abbr = new Label
        {
            Text              = lang.Abbr,
            FontSize          = 16,
            FontAttributes    = FontAttributes.Bold,
            TextColor         = isDark ? Color.FromArgb("#A374FF") : Color.FromArgb("#6D28D9"),
            HorizontalOptions = LayoutOptions.Center
        };
        var name = new Label
        {
            Text              = lang.Name,
            FontSize          = 9,
            TextColor         = isDark ? Color.FromArgb("#9B8EC4") : Color.FromArgb("#5C4F7C"),
            HorizontalOptions = LayoutOptions.Center,
            MaxLines          = 1,
            LineBreakMode     = LineBreakMode.TailTruncation
        };
        var ext = new Label
        {
            Text              = lang.FileExt,
            FontSize          = 8,
            TextColor         = isDark ? Color.FromArgb("#5C5080") : Color.FromArgb("#8B7DC5"),
            HorizontalOptions = LayoutOptions.Center
        };
        var card = new Border
        {
            WidthRequest    = 80,
            HeightRequest   = 84,
            BackgroundColor = isDark ? Color.FromArgb("#1A1435") : Color.FromArgb("#E3DBFF"),
            Stroke          = new SolidColorBrush(isDark ? Color.FromArgb("#5E3BA8") : Color.FromArgb("#7C3AED")),
            StrokeThickness = 1.5,
            StrokeShape     = new RoundRectangle { CornerRadius = 14 },
            Padding         = new Thickness(6, 6),
            Content         = new VerticalStackLayout
            {
                Spacing           = 2,
                VerticalOptions   = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children          = { abbr, name, ext }
            }
        };

        // Tap = quick slot fill
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => MakeGuess(lang);
        card.GestureRecognizers.Add(tap);

        // Pan = drag
        AttachDrag(card, lang);

        return card;
    }

    private void MakeGuess(ProgrammingLanguage lang)
    {
        if (_vm.IsGameOver) return;
        _vm.GuessCommand.Execute(lang);
        RefreshLangCards();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  DRAG & DROP  (adapted from DetektivGame/CraftPage)
    // ─────────────────────────────────────────────────────────────────────────
    private void AttachDrag(Border card, ProgrammingLanguage lang)
    {
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += async (_, e) =>
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                {
                    if (_isDragging) break;
                    _isDragging  = true;
                    _draggedLang = lang;
                    _ = card.FadeToAsync(0.3, 100);
                    _ghostStartPos = GetAbsPos(card);
                    _ghostView = BuildGhost(lang);
                    AbsoluteLayout.SetLayoutBounds(_ghostView,
                        new Rect(_ghostStartPos.X, _ghostStartPos.Y, 80, 80));
                    AbsoluteLayout.SetLayoutFlags(_ghostView, AbsoluteLayoutFlags.None);
                    RootLayout.Add(_ghostView);
                    HighlightDropZone(true);
                    break;
                }

                case GestureStatus.Running:
                {
                    if (_ghostView == null || !_isDragging) break;
                    _ghostView.TranslationX = e.TotalX;
                    _ghostView.TranslationY = e.TotalY;
                    UpdateDropZoneGlow();
                    break;
                }

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                {
                    if (!_isDragging) break;
                    _isDragging = false;
                    _ = card.FadeToAsync(1.0, 100);
                    HighlightDropZone(false);

                    if (HitTestDropZone() && _draggedLang != null)
                        MakeGuess(_draggedLang);

                    if (_ghostView != null)
                    {
                        var g = _ghostView;
                        _ghostView = null;
                        await g.FadeToAsync(0, 80);
                        RootLayout.Remove(g);
                    }
                    _draggedLang = null;
                    break;
                }
            }
        };
        card.GestureRecognizers.Add(pan);
    }

    private Border BuildGhost(ProgrammingLanguage lang) => new()
    {
        WidthRequest    = 80,
        HeightRequest   = 80,
        BackgroundColor = Color.FromArgb("#2A1A50"),
        Stroke          = new SolidColorBrush(Color.FromArgb("#A374FF")),
        StrokeThickness = 2,
        StrokeShape     = new RoundRectangle { CornerRadius = 14 },
        Padding         = new Thickness(6, 8),
        Opacity         = 0.85,
        InputTransparent= true,
        Content         = new VerticalStackLayout
        {
            Spacing           = 3,
            VerticalOptions   = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text           = lang.Abbr,
                    FontSize       = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor      = Color.FromArgb("#A374FF"),
                    HorizontalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text          = lang.Name,
                    FontSize      = 9,
                    TextColor     = Color.FromArgb("#E2D9F3"),
                    HorizontalOptions = LayoutOptions.Center,
                    MaxLines      = 1,
                    LineBreakMode = LineBreakMode.TailTruncation
                }
            }
        }
    };

    private Point GetAbsPos(View view)
    {
        double x = 0, y = 0;
        Element? cur = view;
        while (cur != null && cur != RootLayout)
        {
            if (cur is VisualElement ve) { x += ve.Bounds.X; y += ve.Bounds.Y; }
            cur = cur.Parent;
        }
        return new Point(x, y);
    }

    private bool HitTestDropZone()
    {
        if (_ghostView == null) return false;
        double cx = _ghostStartPos.X + _ghostView.TranslationX + 40;
        double cy = _ghostStartPos.Y + _ghostView.TranslationY + 40;
        var p = GetAbsPos(DropZone);
        return new Rect(p.X, p.Y, DropZone.Width, DropZone.Height).Contains(cx, cy);
    }

    private void UpdateDropZoneGlow()
    {
        DropZone.BackgroundColor = HitTestDropZone()
            ? Color.FromArgb("#2A1A50")
            : Color.FromArgb("#170F2E");
    }

    private void HighlightDropZone(bool on)
    {
        DropZone.Stroke          = new SolidColorBrush(on
            ? Color.FromArgb("#A374FF")
            : Color.FromArgb("#5E3BA8"));
        DropZone.StrokeThickness = on ? 2.5 : 2;
        if (!on) DropZone.BackgroundColor = Color.FromArgb("#170F2E");
    }
}
