using LangGuess.Models;

namespace LangGuess.Services;

public class GameService
{
    private readonly DatabaseService _db;

    public const int MaxAttempts = 6;

    public GameService(DatabaseService db) => _db = db;

    public async Task<ProgrammingLanguage?> GetTodaysLanguageAsync()
    {
        var all = await _db.GetAllLanguagesAsync();
        if (all.Count == 0) return null;
        int index = DateTime.Today.DayOfYear % all.Count;
        return all[index];
    }

    public async Task<ProgrammingLanguage?> GetRandomLanguageAsync(IEnumerable<int> excludeIds)
    {
        var all = await _db.GetAllLanguagesAsync();
        var pool = all.Where(l => !excludeIds.Contains(l.Id)).ToList();
        if (pool.Count == 0) pool = all; // reset if all used
        return pool[Random.Shared.Next(pool.Count)];
    }

    // Defined orderings for categorical columns
    private static readonly string[] ParadigmOrder    = ["Procedural", "OOP", "Multi", "Functional"];
    private static readonly string[] TypingOrder      = ["Static", "Dynamic"];
    private static readonly string[] CompilationOrder = ["Native", "VM", "Script"];
    private static readonly string[] PlatformOrder    = ["System", "Mobile", "Web", "Universal"];
    private static readonly string[] GCTypeOrder      = ["Manual", "Auto"];

    public GuessResultRow Compare(ProgrammingLanguage guess, ProgrammingLanguage secret)
    {
        var yearStatus = CompareYear(guess.Year, secret.Year);

        return new GuessResultRow
        {
            LanguageName      = guess.Name,
            Abbr              = guess.Abbr,
            Year              = guess.Year.ToString(),
            YearStatus        = yearStatus,
            YearArrow         = yearStatus == GuessStatus.Green ? "" : guess.Year < secret.Year ? "↑" : "↓",
            Paradigm          = ShortenParadigm(guess.Paradigm),
            ParadigmStatus    = guess.Paradigm == secret.Paradigm ? GuessStatus.Green : GuessStatus.Red,
            ParadigmArrow     = CatArrow(guess.Paradigm, secret.Paradigm, ParadigmOrder),
            Typing            = guess.Typing == "Dynamic" ? "Dyn" : "Stat",
            TypingStatus      = guess.Typing == secret.Typing ? GuessStatus.Green : GuessStatus.Red,
            TypingArrow       = CatArrow(guess.Typing, secret.Typing, TypingOrder),
            Compilation       = guess.Compilation,
            CompilationStatus = guess.Compilation == secret.Compilation ? GuessStatus.Green : GuessStatus.Red,
            CompilationArrow  = CatArrow(guess.Compilation, secret.Compilation, CompilationOrder),
            Platform          = ShortenPlatform(guess.Platform),
            PlatformStatus    = guess.Platform == secret.Platform ? GuessStatus.Green : GuessStatus.Red,
            PlatformArrow     = CatArrow(guess.Platform, secret.Platform, PlatformOrder),
            // Popularity is now Open Source (Yes/No)
            Popularity        = guess.Popularity,
            PopularityStatus  = guess.Popularity == secret.Popularity ? GuessStatus.Green : GuessStatus.Red,
            PopArrow          = "",
            // GCType is now Manual/Auto
            GCType            = guess.GCType,
            GCTypeStatus      = guess.GCType == secret.GCType ? GuessStatus.Green : GuessStatus.Red,
            GCTypeArrow       = CatArrow(guess.GCType, secret.GCType, GCTypeOrder),
            IsWin             = guess.Id == secret.Id,
        };
    }

    private static string CatArrow(string g, string s, string[] order)
    {
        if (g == s) return "";
        int gi = Array.IndexOf(order, g), si = Array.IndexOf(order, s);
        if (gi < 0 || si < 0) return "";
        return gi < si ? "↑" : "↓";
    }

    private static GuessStatus CompareYear(int g, int s)
    {
        int d = Math.Abs(g - s);
        return d == 0 ? GuessStatus.Green : d <= 5 ? GuessStatus.Yellow : GuessStatus.Red;
    }

    private static string ShortenParadigm(string p) => p switch
    {
        "Procedural" => "Proc",
        "Functional" => "Func",
        _ => p
    };

    private static string ShortenPlatform(string p) => p switch
    {
        "Universal" => "All",
        "System"    => "Sys",
        "Mobile"    => "Mob",
        _ => p
    };
}
