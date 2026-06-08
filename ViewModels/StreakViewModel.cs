using System.Collections.ObjectModel;
using System.Windows.Input;
using LangGuess.Models;
using LangGuess.Services;

namespace LangGuess.ViewModels;

/// <summary>
/// Score Mode:
///   - Normal: 6 attempts, score tracked separately
///   - Hard:   3 attempts, score tracked separately
/// Score RESETS to 0 on every loss.
/// No history saved (only Daily mode saves history).
/// </summary>
public class StreakViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly GameService     _game;

    private ProgrammingLanguage?       _secret;
    private List<ProgrammingLanguage>? _allLanguages;
    private readonly HashSet<int>      _usedIds = new();

    private const string HardModeKey = "score_hard_mode";

    private int    _scoreNormal;
    private int    _scoreHard;
    private bool   _isWon;
    private bool   _isLost;
    private bool   _isGameOver;
    private string _statusMessage = "";

    public ObservableCollection<GuessResultRow>      GuessRows          { get; } = new();
    public ObservableCollection<ProgrammingLanguage> AvailableLanguages { get; } = new();

    // ── Two separate score counters ──────────────────────────────────────────
    public int ScoreNormal { get => _scoreNormal; private set => SetField(ref _scoreNormal, value); }
    public int ScoreHard   { get => _scoreHard;   private set => SetField(ref _scoreHard,   value); }

    // ── Hard mode toggle (persisted) ─────────────────────────────────────────
    public bool IsHardMode
    {
        get => Preferences.Get(HardModeKey, false);
        set
        {
            Preferences.Set(HardModeKey, value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(MaxAttempts));
            OnPropertyChanged(nameof(AttemptsDisplay));
            _ = LoadNextLanguageAsync();
        }
    }

    public int MaxAttempts => IsHardMode ? 3 : 6;

    // ── Other VM state ───────────────────────────────────────────────────────
    public bool   IsWon         { get => _isWon;         private set => SetField(ref _isWon, value); }
    public bool   IsLost        { get => _isLost;        private set => SetField(ref _isLost, value); }
    public bool   IsGameOver    { get => _isGameOver;    private set => SetField(ref _isGameOver, value); }
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }

    public int    AttemptsLeft    => MaxAttempts - GuessRows.Count;
    public string AttemptsDisplay => $"{AttemptsLeft}/{MaxAttempts}";

    public ICommand GuessCommand        { get; }
    public ICommand BackCommand         { get; }
    public ICommand NextCommand         { get; }
    public ICommand RestartCommand      { get; }
    public ICommand GoToSettingsCommand { get; }

    public StreakViewModel(DatabaseService db, GameService game)
    {
        _db   = db;
        _game = game;

        GuessCommand        = new RelayCommand<ProgrammingLanguage>(OnGuess, _ => !IsGameOver);
        BackCommand         = new RelayCommand(() => Shell.Current.GoToAsync("//HomePage"));
        NextCommand         = new RelayCommand(async () => await LoadNextLanguageAsync(), () => IsGameOver);
        RestartCommand      = new RelayCommand(async () => await RestartAsync());
        GoToSettingsCommand = new RelayCommand(() => Shell.Current.GoToAsync("//SettingsPage?from=streak"));
    }

    public async Task InitAsync()
    {
        _allLanguages = await _db.GetAllLanguagesAsync();
        _usedIds.Clear();
        ScoreNormal = 0;
        ScoreHard   = 0;
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
        OnPropertyChanged(nameof(AttemptsDisplay));
        ((RelayCommand<ProgrammingLanguage>)GuessCommand).RaiseCanExecuteChanged();
        ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
    }

    private async void OnGuess(ProgrammingLanguage? lang)
    {
        if (lang == null || _secret == null || IsGameOver) return;

        AvailableLanguages.Remove(lang);

        var row = _game.Compare(lang, _secret);
        GuessRows.Add(row);
        OnPropertyChanged(nameof(AttemptsLeft));
        OnPropertyChanged(nameof(AttemptsDisplay));

        if (row.IsWin)
        {
            if (IsHardMode) ScoreHard++;
            else            ScoreNormal++;

            IsWon         = true;
            IsGameOver    = true;
            StatusMessage = _secret.Name;
        }
        else if (GuessRows.Count >= MaxAttempts)
        {
            // ── SCORE RESETS ON LOSS ────────────────────────────────────────
            if (IsHardMode) ScoreHard   = 0;
            else            ScoreNormal = 0;

            IsLost        = true;
            IsGameOver    = true;
            StatusMessage = _secret.Name;
        }

        ((RelayCommand<ProgrammingLanguage>)GuessCommand).RaiseCanExecuteChanged();
        ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
    }

    private async Task RestartAsync()
    {
        _usedIds.Clear();
        ScoreNormal = 0;
        ScoreHard   = 0;
        await LoadNextLanguageAsync();
    }
}
