using System.Collections.ObjectModel;
using System.Windows.Input;
using LangGuess.Models;
using LangGuess.Services;

namespace LangGuess.ViewModels;

public class GameViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly GameService     _game;
    private readonly AudioService    _audio;

    private ProgrammingLanguage? _secret;
    private List<ProgrammingLanguage>? _allLanguages;

    private int         _streak;
    private bool        _isWon;
    private bool        _isGameOver;
    private string      _statusMessage    = "";
    private bool        _alreadyPlayed;
    private GameHistory? _todayResult;

    public ObservableCollection<GuessResultRow>    GuessRows          { get; } = new();
    public ObservableCollection<ProgrammingLanguage> AvailableLanguages { get; } = new();
    public List<ProgrammingLanguage>               AllLanguages        { get; private set; } = new();

    public int    Streak          { get => _streak;         private set => SetField(ref _streak, value); }
    public bool   IsWon           { get => _isWon;          private set => SetField(ref _isWon, value); }
    public bool   IsGameOver      { get => _isGameOver;     private set { SetField(ref _isGameOver, value); OnPropertyChanged(nameof(IsLost)); } }
    public bool   IsLost          => IsGameOver && !IsWon;
    public string StatusMessage   { get => _statusMessage;  private set => SetField(ref _statusMessage, value); }
    public int    AttemptsLeft    => GameService.MaxAttempts - GuessRows.Count;

    // ── Already-played-today state ──────────────────────────────────────────
    public bool         AlreadyPlayed { get => _alreadyPlayed; private set => SetField(ref _alreadyPlayed, value); }
    public GameHistory? TodayResult   { get => _todayResult;   private set => SetField(ref _todayResult, value); }

    // Shortcuts for the "already played" banner
    public string  TodayLang     => TodayResult?.SecretLanguage ?? "";
    public bool    TodayWon      => TodayResult?.IsWon ?? false;
    public string  TodayAttempts => TodayResult != null ? $"{TodayResult.Attempts}/6" : "";

    public ICommand GuessCommand        { get; }
    public ICommand GoToSettingsCommand { get; }
    public ICommand BackCommand         { get; }
    public ICommand GoToScoreCommand    { get; }

    public GameViewModel(DatabaseService db, GameService game, AudioService audio)
    {
        _db    = db;
        _game  = game;
        _audio = audio;

        GuessCommand        = new RelayCommand<ProgrammingLanguage>(OnGuess, _ => !IsGameOver && !AlreadyPlayed);
        GoToSettingsCommand = new RelayCommand(() => Shell.Current.GoToAsync("//SettingsPage?from=game"));
        BackCommand         = new RelayCommand(() => Shell.Current.GoToAsync("//HomePage"));
        GoToScoreCommand    = new RelayCommand(() => Shell.Current.GoToAsync("//StreakPage"));
    }

    public async Task InitAsync()
    {
        _allLanguages ??= await _db.GetAllLanguagesAsync();
        AllLanguages = _allLanguages;

        // Check if already played today
        TodayResult   = await _db.GetTodayHistoryAsync();
        AlreadyPlayed = TodayResult != null;
        Streak        = await _db.GetStreakAsync();

        if (!AlreadyPlayed)
        {
            _secret = await _game.GetTodaysLanguageAsync();
            AvailableLanguages.Clear();
            foreach (var l in _allLanguages) AvailableLanguages.Add(l);
        }

        OnPropertyChanged(nameof(TodayLang));
        OnPropertyChanged(nameof(TodayWon));
        OnPropertyChanged(nameof(TodayAttempts));
        ((RelayCommand<ProgrammingLanguage>)GuessCommand).RaiseCanExecuteChanged();
    }

    private async void OnGuess(ProgrammingLanguage? lang)
    {
        if (lang == null || _secret == null || IsGameOver || AlreadyPlayed) return;

        await _audio.PlayTapAsync();
        AvailableLanguages.Remove(lang);

        var row = _game.Compare(lang, _secret);
        GuessRows.Add(row);
        OnPropertyChanged(nameof(AttemptsLeft));

        if (row.IsWin)
        {
            IsWon = true;
            IsGameOver = true;
            StatusMessage = _secret.Name;
            await _audio.PlayWinAsync();
            await SaveHistoryAsync(true);
            Streak = await _db.GetStreakAsync();
        }
        else if (GuessRows.Count >= GameService.MaxAttempts)
        {
            IsGameOver = true;
            StatusMessage = _secret.Name;
            OnPropertyChanged(nameof(IsLost));
            await _audio.PlayWrongAsync();
            // Saves loss → daily streak is broken (GetStreakAsync will see IsWon=false)
            await SaveHistoryAsync(false);
            Streak = 0;
        }
        else
        {
            bool anyGreen = row.YearStatus == GuessStatus.Green || row.ParadigmStatus == GuessStatus.Green;
            if (anyGreen) await _audio.PlayCorrectAsync();
            else          await _audio.PlayWrongAsync();
        }

        ((RelayCommand<ProgrammingLanguage>)GuessCommand).RaiseCanExecuteChanged();
    }

    private async Task SaveHistoryAsync(bool won)
    {
        var guessed = string.Join(",", GuessRows.Select(r => r.LanguageName));
        await _db.SaveHistoryAsync(new GameHistory
        {
            Date             = DateTime.Today.ToString("yyyy-MM-dd"),
            SecretLanguage   = _secret!.Name,
            Attempts         = GuessRows.Count,
            IsWon            = won,
            GuessedLanguages = guessed
        });
    }
}
