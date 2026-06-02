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
    private int _streak;
    private bool _isWon;
    private bool _isGameOver;
    private string _statusMessage = "";
    private bool _alreadyPlayedToday;

    public ObservableCollection<GuessResultRow> GuessRows { get; } = new();
    public ObservableCollection<ProgrammingLanguage> AvailableLanguages { get; } = new();

    public int  Streak              { get => _streak;           private set => SetField(ref _streak, value); }
    public bool IsWon               { get => _isWon;            private set => SetField(ref _isWon, value); }
    public bool IsGameOver          { get => _isGameOver;       private set => SetField(ref _isGameOver, value); }
    public string StatusMessage     { get => _statusMessage;    private set => SetField(ref _statusMessage, value); }
    public bool AlreadyPlayedToday  { get => _alreadyPlayedToday; private set => SetField(ref _alreadyPlayedToday, value); }
    public int AttemptsLeft         => GameService.MaxAttempts - GuessRows.Count;
    public string? SecretName       => IsGameOver ? _secret?.Name : null;

    public ICommand GuessCommand        { get; }
    public ICommand GoToSettingsCommand { get; }
    public ICommand BackCommand         { get; }

    public GameViewModel(DatabaseService db, GameService game, AudioService audio)
    {
        _db    = db;
        _game  = game;
        _audio = audio;

        GuessCommand        = new RelayCommand<ProgrammingLanguage>(OnGuess, _ => !IsGameOver);
        GoToSettingsCommand = new RelayCommand(() => Shell.Current.GoToAsync("//SettingsPage"));
        BackCommand         = new RelayCommand(() => Shell.Current.GoToAsync("//HomePage"));
    }

    public async Task InitAsync()
    {
        _secret = await _game.GetTodaysLanguageAsync();
        Streak  = await _db.GetStreakAsync();

        var all = await _db.GetAllLanguagesAsync();
        AvailableLanguages.Clear();
        foreach (var l in all) AvailableLanguages.Add(l);

        // Check if already played today
        var history = await _db.GetTodayHistoryAsync();
        if (history != null)
        {
            AlreadyPlayedToday = true;
            IsGameOver = true;
            IsWon = history.IsWon;
            StatusMessage = history.IsWon
                ? $"Juba mängitud! Vastus: {history.SecretLanguage}"
                : $"Kaotasid eile. Vastus oli: {history.SecretLanguage}";
        }
    }

    private async void OnGuess(ProgrammingLanguage? lang)
    {
        if (lang == null || _secret == null || IsGameOver) return;

        // Remove from available so it can't be guessed again
        AvailableLanguages.Remove(lang);

        var row = _game.Compare(lang, _secret);
        GuessRows.Add(row);
        OnPropertyChanged(nameof(AttemptsLeft));

        if (row.IsWin)
        {
            IsWon = true;
            IsGameOver = true;
            StatusMessage = "Õige! Arvasid ära! 🎉";
            await _audio.PlayWinAsync();
            await SaveHistoryAsync(true);
            Streak = await _db.GetStreakAsync();
        }
        else if (GuessRows.Count >= GameService.MaxAttempts)
        {
            IsGameOver = true;
            StatusMessage = $"Vastus oli: {_secret.Name}";
            OnPropertyChanged(nameof(SecretName));
            await _audio.PlayWrongAsync();
            await SaveHistoryAsync(false);
        }
        else
        {
            bool anyGreen = row.YearStatus    == GuessStatus.Green ||
                            row.ParadigmStatus == GuessStatus.Green ||
                            row.TypingStatus  == GuessStatus.Green;
            if (anyGreen)
                await _audio.PlayCorrectAsync();
            else
                await _audio.PlayWrongAsync();
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
