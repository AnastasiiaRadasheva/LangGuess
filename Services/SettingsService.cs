using System.ComponentModel;

namespace LangGuess.Services;

public class SettingsService : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private const string LangKey      = "ui_language";
    private const string ThemeKey     = "app_theme";
    private const string SoundKey     = "sound_enabled";
    private const string NameKey      = "player_name";
    private const string MusicVolKey  = "music_volume";
    private const string SfxKey       = "sfx_enabled";

    private string _language;
    private bool   _isDark;
    private bool   _soundEnabled;
    private string _playerName;
    private double _musicVolume;
    private bool   _sfxEnabled;

    public SettingsService()
    {
        _language     = Preferences.Get(LangKey,     "en");
        _isDark       = Preferences.Get(ThemeKey,    "dark") == "dark";
        _soundEnabled = Preferences.Get(SoundKey,    true);
        _playerName   = Preferences.Get(NameKey,     "");
        _musicVolume  = Preferences.Get(MusicVolKey, 0.5);
        _sfxEnabled   = Preferences.Get(SfxKey,      true);
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

    public string PlayerName
    {
        get => _playerName;
        set { _playerName = value?.Trim() ?? ""; Preferences.Set(NameKey, _playerName); Notify(); }
    }

    public bool HasName => !string.IsNullOrEmpty(_playerName);

    public double MusicVolume
    {
        get => _musicVolume;
        set { _musicVolume = Math.Clamp(value, 0.0, 1.0); Preferences.Set(MusicVolKey, _musicVolume); Notify(); }
    }

    public bool SfxEnabled
    {
        get => _sfxEnabled;
        set { _sfxEnabled = value; Preferences.Set(SfxKey, value); Notify(); }
    }

    private void Notify([System.Runtime.CompilerServices.CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
