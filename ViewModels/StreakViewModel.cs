using System.Collections.ObjectModel;
using System.Windows.Input;
using LangGuess.Models;
using LangGuess.Services;

namespace LangGuess.ViewModels;

/// <summary>
/// Score Mode: guess random languages, earn +1 for each correct guess.
/// No streak — a failed language just moves to the next one.
/// History is NOT saved (only Daily mode saves history).
/// </summary>
public class StreakViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly GameService     _game;
    private readonly AudioService    _audio;

    private ProgrammingLanguage?       _secret;
    private List<ProgrammingLanguage>? _allLanguages;
    private readonly HashSet<int>      _usedIds = new();

    private int    _score;
    private bool   _isWon;
    private bool   _isLost;
    private bool   _isGameOver;
    private string _statusMessage = "";

    public ObservableCollection<GuessResultRow>      GuessRows          { get; } = new();
    public ObservableCollection<ProgrammingLanguage> AvailableLanguages { get; } = new();

    public int    Score         { get => _score;         private set => SetField(ref _score, value); }
    public bool   IsWon         { get => _isWon;         private set => SetField(ref _isWon, value); }
    public bool   IsLost        { get => _isLost;        private set => SetField(ref _isLost, value); }
    public bool   IsGameOver    { get => _isGameOver;    private set => SetField(ref _isGameOver, value); }
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }
    public int    AttemptsLeft  => GameService.MaxAttempts - GuessRows.Count;

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
        NextCommand         = new RelayCommand(async () => await LoadNextLanguageAsync(), () => IsGameOver);
        RestartCommand      = new RelayCommand(async () => await RestartAsync());
        GoToSettingsCommand = new RelayCommand(() => Shell.Current.GoToAsync("//SettingsPage?from=game"));
    }

    public async Task InitAsync()
    {
        _allLanguages = await _db.GetAllLanguagesAsync();
        _usedIds.Clear();
        Score = 0;
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
            Score++;
            IsWon      = true;
            IsGameOver = true;
            StatusMessage = _secret.Name;
            await _audio.PlayWinAsync();
        }
        else if (GuessRows.Count >= GameService.MaxAttempts)
        {
            // Missed this language — no score penalty, just show answer and move on
            IsLost     = true;
            IsGameOver = true;
            StatusMessage = _secret.Name;
            await _audio.PlayWrongAsync();
        }
        else
        {
            bool anyGreen = row.YearStatus == GuessStatus.Green || row.ParadigmStatus == GuessStatus.Green;
            if (anyGreen) await _audio.PlayCorrectAsync();
            else          await _audio.PlayWrongAsync();
        }

        ((RelayCommand<ProgrammingLanguage>)GuessCommand).RaiseCanExecuteChanged();
        ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
    }

    private async Task RestartAsync()
    {
        _usedIds.Clear();
        Score = 0;
        await LoadNextLanguageAsync();
    }
}
