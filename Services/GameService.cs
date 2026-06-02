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

    public GuessResultRow Compare(ProgrammingLanguage guess, ProgrammingLanguage secret)
    {
        var yearStatus = CompareYear(guess.Year, secret.Year);
        var popStatus  = ComparePopularity(guess.Popularity, secret.Popularity);

        return new GuessResultRow
        {
            LanguageName      = guess.Name,
            Abbr              = guess.Abbr,
            Year              = guess.Year.ToString(),
            YearStatus        = yearStatus,
            YearArrow         = yearStatus == GuessStatus.Green ? "" : guess.Year < secret.Year ? "↑" : "↓",
            Paradigm          = ShortenParadigm(guess.Paradigm),
            ParadigmStatus    = guess.Paradigm == secret.Paradigm ? GuessStatus.Green : GuessStatus.Red,
            Typing            = guess.Typing == "Dynamic" ? "Dyn" : "Stat",
            TypingStatus      = guess.Typing == secret.Typing ? GuessStatus.Green : GuessStatus.Red,
            Compilation       = ShortenCompilation(guess.Compilation),
            CompilationStatus = guess.Compilation == secret.Compilation ? GuessStatus.Green : GuessStatus.Red,
            Platform          = ShortenPlatform(guess.Platform),
            PlatformStatus    = guess.Platform == secret.Platform ? GuessStatus.Green : GuessStatus.Red,
            Popularity        = guess.Popularity,
            PopularityStatus  = popStatus,
            PopArrow          = PopDirection(guess.Popularity, secret.Popularity, popStatus),
            GCType            = guess.GCType,
            GCTypeStatus      = guess.GCType == secret.GCType ? GuessStatus.Green : GuessStatus.Red,
            IsWin             = guess.Id == secret.Id,
        };
    }

    private static GuessStatus CompareYear(int g, int s)
    {
        int d = Math.Abs(g - s);
        return d == 0 ? GuessStatus.Green : d <= 5 ? GuessStatus.Yellow : GuessStatus.Red;
    }

    private static GuessStatus ComparePopularity(string g, string s)
    {
        if (g == s) return GuessStatus.Green;
        string[] order = ["Top5", "Top20", "Niche"];
        int gi = Array.IndexOf(order, g), si = Array.IndexOf(order, s);
        return Math.Abs(gi - si) == 1 ? GuessStatus.Yellow : GuessStatus.Red;
    }

    // ↑ = secret is MORE popular (lower index), ↓ = less popular
    private static string PopDirection(string g, string s, GuessStatus status)
    {
        if (status == GuessStatus.Green) return "";
        string[] order = ["Top5", "Top20", "Niche"];
        int gi = Array.IndexOf(order, g), si = Array.IndexOf(order, s);
        return gi > si ? "↑" : "↓";
    }

    private static string ShortenParadigm(string p) => p switch
    {
        "Procedural" => "Proc",
        "Functional" => "Func",
        _ => p
    };

    private static string ShortenCompilation(string c) => c switch
    {
        "Interpreted" => "Interp",
        "Transpiled"  => "Trans",
        "Compiled"    => "Comp",
        _ => c
    };

    private static string ShortenPlatform(string p) => p switch
    {
        "Universal" => "Uni",
        "System"    => "Sys",
        "Mobile"    => "Mob",
        _ => p
    };
}
