using System.Collections.ObjectModel;
using System.Windows.Input;
using LangGuess.Models;
using LangGuess.Services;

namespace LangGuess.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly SettingsService    _settings;
    private readonly LocalizationService _loc;
    private readonly DatabaseService    _db;

    public ObservableCollection<GameHistory> History { get; } = new();

    // Language options exposed for Picker
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

    private bool _soundEnabled;
    public bool SoundEnabled
    {
        get => _soundEnabled;
        set { SetField(ref _soundEnabled, value); _settings.SoundEnabled = value; }
    }

    public ICommand BackCommand         { get; }
    public ICommand ClearHistoryCommand { get; }

    public bool HasHistory => History.Count > 0;

    public SettingsViewModel(SettingsService settings, LocalizationService loc, DatabaseService db)
    {
        _settings = settings;
        _loc      = loc;
        _db       = db;

        _isDark       = settings.IsDark;
        _soundEnabled = settings.SoundEnabled;
        _selectedLanguageIndex = LanguageCodes.IndexOf(settings.Language);
        if (_selectedLanguageIndex < 0) _selectedLanguageIndex = 0;

        BackCommand = new RelayCommand(() => Shell.Current.GoToAsync("//HomePage"));
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
