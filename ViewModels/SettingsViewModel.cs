using System.Collections.ObjectModel;
using System.Windows.Input;
using LangGuess.Models;
using LangGuess.Services;

namespace LangGuess.ViewModels;

[QueryProperty(nameof(From), "from")]
public class SettingsViewModel : BaseViewModel
{
    private readonly SettingsService     _settings;
    private readonly LocalizationService _loc;
    private readonly DatabaseService     _db;
    private readonly AudioService        _audio;

    public ObservableCollection<GameHistory> History { get; } = new();

    public List<string> LanguageOptions { get; } = ["English", "Eesti", "Русский"];
    public List<string> LanguageCodes   { get; } = ["en", "et", "ru"];

    private int _selectedLanguageIndex;
    public int SelectedLanguageIndex
    {
        get => _selectedLanguageIndex;
        set
        {
            SetField(ref _selectedLanguageIndex, value);
            var code = LanguageCodes[value];
            _settings.Language = code;
            _loc.SetLanguage(code);
        }
    }

    private bool _isDark;
    public bool IsDark
    {
        get => _isDark;
        set { SetField(ref _isDark, value); _settings.IsDark = value; }
    }

    private bool _musicEnabled;
    public bool MusicEnabled
    {
        get => _musicEnabled;
        set
        {
            if (!SetField(ref _musicEnabled, value)) return;
            _audio.MusicEnabled = value;
        }
    }

    private double _musicVolume;
    public double MusicVolume
    {
        get => _musicVolume;
        set
        {
            if (!SetField(ref _musicVolume, value)) return;
            _settings.MusicVolume = value; // keeps SettingsService in sync so next visit is correct
            _audio.MusicVolume = value;
        }
    }

    private string _playerName = "";
    public string PlayerName
    {
        get => _playerName;
        set { SetField(ref _playerName, value); }
    }

    public ICommand BackCommand         { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand SaveNameCommand     { get; }

    public bool HasHistory => History.Count > 0;

    private string _returnRoute = "//HomePage";
    public string From
    {
        set => _returnRoute = value switch
        {
            "game"   => "//GamePage",
            "streak" => "//StreakPage",
            _        => "//HomePage"
        };
    }

    public SettingsViewModel(SettingsService settings, LocalizationService loc,
                             DatabaseService db, AudioService audio)
    {
        _settings = settings;
        _loc      = loc;
        _db       = db;
        _audio    = audio;

        _isDark                = settings.IsDark;
        _musicEnabled          = audio.MusicEnabled;
        _musicVolume           = audio.MusicVolume; // read from Preferences directly — always fresh
        _playerName            = settings.PlayerName;
        _selectedLanguageIndex = LanguageCodes.IndexOf(settings.Language);
        if (_selectedLanguageIndex < 0) _selectedLanguageIndex = 0;

        BackCommand = new RelayCommand(() => Shell.Current.GoToAsync(_returnRoute));
        SaveNameCommand = new RelayCommand(() => _settings.PlayerName = _playerName);
        ClearHistoryCommand = new RelayCommand(async () =>
        {
            await _db.ClearAllHistoryAsync();
            History.Clear();
            OnPropertyChanged(nameof(HasHistory));
        });
    }

    public async Task LoadHistoryAsync()
    {
        var all = await _db.GetAllHistoryAsync();
        History.Clear();
        foreach (var h in all) History.Add(h);
        OnPropertyChanged(nameof(HasHistory));
    }
}
