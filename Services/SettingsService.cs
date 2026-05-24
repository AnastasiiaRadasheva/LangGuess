using System.ComponentModel;

namespace LangGuess.Services;

public class SettingsService : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private const string LangKey  = "ui_language";
    private const string ThemeKey = "app_theme";
    private const string SoundKey = "sound_enabled";

    private string _language;
    private bool _isDark;
    private bool _soundEnabled;

    public SettingsService()
    {
        _language     = Preferences.Get(LangKey,  "en");
        _isDark       = Preferences.Get(ThemeKey, "dark") == "dark";
        _soundEnabled = Preferences.Get(SoundKey, true);
    }

    public string Language
    {
        get => _language;
        set { _language = value; Preferences.Set(LangKey, value); Notify(); }
    }

    public bool IsDark
    {
        get => _isDark;
        set
        {
            _isDark = value;
            Preferences.Set(ThemeKey, value ? "dark" : "light");
            if (Application.Current != null)
                Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
            Notify();
        }
    }

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set { _soundEnabled = value; Preferences.Set(SoundKey, value); Notify(); }
    }

    private void Notify([System.Runtime.CompilerServices.CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
