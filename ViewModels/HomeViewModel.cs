using System.Windows.Input;
using LangGuess.Services;

namespace LangGuess.ViewModels;

public class HomeViewModel : BaseViewModel
{
    private readonly SettingsService     _settings;
    private readonly LocalizationService _loc;

    public List<string> LanguageCodes { get; } = ["en", "et", "ru"];

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
            OnPropertyChanged(nameof(Lang0Color)); OnPropertyChanged(nameof(Lang0Text));
            OnPropertyChanged(nameof(Lang1Color)); OnPropertyChanged(nameof(Lang1Text));
            OnPropertyChanged(nameof(Lang2Color)); OnPropertyChanged(nameof(Lang2Text));
        }
    }

    public Color Lang0Color => ButtonBg(0);
    public Color Lang1Color => ButtonBg(1);
    public Color Lang2Color => ButtonBg(2);
    public Color Lang0Text  => ButtonFg(0);
    public Color Lang1Text  => ButtonFg(1);
    public Color Lang2Text  => ButtonFg(2);

    private Color ButtonBg(int idx) => idx == _selectedLanguageIndex
        ? Color.FromArgb("#5E3BA8")
        : Color.FromArgb("#2A1F45");

    private Color ButtonFg(int idx) => idx == _selectedLanguageIndex
        ? Colors.White
        : Color.FromArgb("#9B8EC4");

    private bool _isDark;
    public bool IsDark
    {
        get => _isDark;
        set { SetField(ref _isDark, value); _settings.IsDark = value; }
    }

    // Player name support
    public string PlayerName    => _settings.PlayerName;
    public bool   HasName       => _settings.HasName;
    public bool   HasNoName     => !_settings.HasName;

    private string _nameInput = "";
    public string NameInput
    {
        get => _nameInput;
        set => SetField(ref _nameInput, value);
    }

    public ICommand PlayCommand         { get; }
    public ICommand StreakCommand       { get; }
    public ICommand GoToSettingsCommand { get; }
    public ICommand SelectLangCommand   { get; }
    public ICommand ConfirmNameCommand  { get; }

    public HomeViewModel(SettingsService settings, LocalizationService loc)
    {
        _settings = settings;
        _loc      = loc;

        _isDark = settings.IsDark;
        _selectedLanguageIndex = LanguageCodes.IndexOf(settings.Language);
        if (_selectedLanguageIndex < 0) _selectedLanguageIndex = 0;

        PlayCommand         = new RelayCommand(() => Shell.Current.GoToAsync("//GamePage"));
        StreakCommand       = new RelayCommand(() => Shell.Current.GoToAsync("//StreakPage"));
        GoToSettingsCommand = new RelayCommand(() => Shell.Current.GoToAsync("//SettingsPage?from=home"));
        SelectLangCommand   = new RelayCommand<string>(idx =>
        {
            if (int.TryParse(idx, out int i))
                SelectedLanguageIndex = i;
        });
        ConfirmNameCommand  = new RelayCommand(() =>
        {
            if (string.IsNullOrWhiteSpace(_nameInput)) return;
            _settings.PlayerName = _nameInput.Trim();
            OnPropertyChanged(nameof(PlayerName));
            OnPropertyChanged(nameof(HasName));
            OnPropertyChanged(nameof(HasNoName));
        });
    }
}
