using System.Collections.ObjectModel;
using System.Windows.Input;
using LangGuess.Models;
using LangGuess.Services;

namespace LangGuess.ViewModels;

public class StreakViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly GameService     _game;
    private readonly AudioService    _audio;

    private ProgrammingLanguage? _secret;
    private List<ProgrammingLanguage>? _allLanguages;
    private readonly HashSet<int> _usedIds = new();

    private int    _currentStreak;
    private int    _bestStreak;
    private bool   _isWon;
    private bool   _isLost;
    private bool   _isGameOver;
    private string _statusMessage = "";

    public ObservableCollection<GuessResultRow> GuessRows          { get; } = new();
    public ObservableCollection<ProgrammingLanguage> AvailableLanguages { get; } = new();

    public int    CurrentStreak  { get => _currentStreak;  private set => SetField(ref _currentStreak, value); }
    public int    BestStreak     { get => _bestStreak;     private set => SetField(ref _bestStreak, value); }
    public bool   IsWon          { get => _isWon;          private set => SetField(ref _isWon, value); }
    public bool   IsLost         { get => _isLost;         private set => SetField(ref _isLost, value); }
    public bool   IsGameOver     { get => _isGameOver;     private set => SetField(ref _isGameOver, value); }
    public string StatusMessage  { get => _statusMessage;  private set => SetField(ref _statusMessage, value); }
    public int    AttemptsLeft   => GameService.MaxAttempts - GuessRows.Count;

    public ICommand GuessCommand        { get; }
    public ICommand BackCommand         { get; }
    public ICommand NextCommand         { get; }
    public ICommand RestartCommand      { get; }
    public ICommand GoToSettingsCommand { get; }

    public StreakViewModel(DatabaseService db, GameService game, AudioService audio)
    {
        _db    = db;
        _game  = game;
        _audio = audio;

        GuessCommand        = new RelayCommand<ProgrammingLanguage>(OnGuess, _ => !IsGameOver);
        BackCommand         = new RelayCommand(() => Shell.Current.GoToAsync("//HomePage"));
        NextCommand         = new RelayCommand(async () => await LoadNextLanguageAsync(), () => IsWon);
        RestartCommand      = new RelayCommand(async () => await RestartAsync());
        GoToSettingsCommand = new RelayCommand(() => Shell.Current.GoToAsync("//SettingsPage?from=game"));
    }

    public async Task InitAsync()
    {
        _bestStreak = Preferences.Get("streak_best", 0);
        OnPropertyChanged(nameof(BestStreak));
        _allLanguages = await _db.GetAllLanguagesAsync();
        _usedIds.Clear();
        CurrentStreak = 0;
        await LoadNextLanguageAsync();
    }

    private async Task LoadNextLanguageAsync()
    {
        _allLanguages ??= await _db.GetAllLanguagesAsync();
        _secret = await _game.GetRandomLanguageAsync(_usedIds);
        if (_secret != null) _usedIds.Add(_secret.Id);

        GuessRows.Clear();
        IsGameOver    = false;
        IsWon         = false;
        IsLost        = false;
        StatusMessage = "";

        AvailableLanguages.Clear();
        foreach (var l in _allLanguages) AvailableLanguages.Add(l);

        OnPropertyChanged(nameof(AttemptsLeft));
        ((RelayCommand<ProgrammingLanguage>)GuessCommand).RaiseCanExecuteChanged();
        ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
    }

    private async void OnGuess(ProgrammingLanguage? lang)
    {
        if (lang == null || _secret == null || IsGameOver) return;

        await _audio.PlayTapAsync();
        AvailableLanguages.Remove(lang);

        var row = _game.Compare(lang, _secret);
        GuessRows.Add(row);
        OnPropertyChanged(nameof(AttemptsLeft));

        if (row.IsWin)
        {
            CurrentStreak++;
            if (CurrentStreak > _bestStreak)
            {
                _bestStreak = CurrentStreak;
                Preferences.Set("streak_best", _bestStreak);
                OnPropertyChanged(nameof(BestStreak));
            }
            IsWon      = true;
            IsGameOver = true;
            StatusMessage = _secret.Name;
            await _audio.PlayWinAsync();
        }
        else if (GuessRows.Count >= GameService.MaxAttempts)
        {
            IsLost     = true;
            IsGameOver = true;
            StatusMessage = _secret.Name;
            await _audio.PlayWrongAsync();
            // Streak broken — reset but keep best
            CurrentStreak = 0;
        }
        else
        {
            bool anyGreen = row.YearStatus    == GuessStatus.Green ||
                            row.ParadigmStatus == GuessStatus.Green;
            if (anyGreen) await _audio.PlayCorrectAsync();
            else          await _audio.PlayWrongAsync();
        }

        ((RelayCommand<ProgrammingLanguage>)GuessCommand).RaiseCanExecuteChanged();
        ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
    }

    private async Task RestartAsync()
    {
        _usedIds.Clear();
        CurrentStreak = 0;
        await LoadNextLanguageAsync();
    }
}
