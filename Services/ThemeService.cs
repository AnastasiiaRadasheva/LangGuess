namespace LangGuess.Services;

public class ThemeService
{
    private const string ThemeKey = "app_theme";

    public bool IsDark { get; private set; }

    public ThemeService()
    {
        var saved = Preferences.Get(ThemeKey, "dark");
        IsDark = saved == "dark";
        Apply();
    }

    public void SetDark(bool dark)
    {
        IsDark = dark;
        Preferences.Set(ThemeKey, dark ? "dark" : "light");
        Apply();
    }

    private void Apply()
    {
        if (Application.Current == null) return;
        Application.Current.UserAppTheme = IsDark ? AppTheme.Dark : AppTheme.Light;
    }
}
