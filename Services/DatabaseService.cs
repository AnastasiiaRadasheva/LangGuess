using LangGuess.Models;
using SQLite;

namespace LangGuess.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    private const int DbVersion = 5;

    // 12 well-known languages — small enough to guess from a card list
    private static readonly List<ProgrammingLanguage> SeedLanguages = new()
    {
        new() { Name="Python",     Year=1991, Paradigm="Multi",      Typing="Dynamic", Compilation="Script",  Platform="Universal", Popularity="Yes", GCType="Auto",   FileExt=".py",    Abbr="PY",  Description="Created by Guido van Rossum. Famous for readable syntax and a massive library ecosystem used in AI, science, and web." },
        new() { Name="Java",       Year=1995, Paradigm="OOP",        Typing="Static",  Compilation="VM",      Platform="Universal", Popularity="Yes", GCType="Auto",   FileExt=".java",  Abbr="JV",  Description="Write once, run anywhere. Powers Android apps, enterprise backends, and runs on the JVM." },
        new() { Name="C",          Year=1972, Paradigm="Procedural", Typing="Static",  Compilation="Native",  Platform="System",    Popularity="Yes", GCType="Manual", FileExt=".c",     Abbr="C",   Description="The mother of modern languages. Born at Bell Labs in 1972. Still drives operating systems and embedded chips." },
        new() { Name="C++",        Year=1985, Paradigm="Multi",      Typing="Static",  Compilation="Native",  Platform="System",    Popularity="Yes", GCType="Manual", FileExt=".cpp",   Abbr="C++", Description="C with superpowers. Used in AAA games, browsers, compilers, and real-time systems where speed matters." },
        new() { Name="JavaScript", Year=1995, Paradigm="Multi",      Typing="Dynamic", Compilation="Script",  Platform="Web",       Popularity="Yes", GCType="Auto",   FileExt=".js",    Abbr="JS",  Description="The language of the web. Runs in every browser on Earth. Brendan Eich created it in just 10 days." },
        new() { Name="C#",         Year=2000, Paradigm="Multi",      Typing="Static",  Compilation="VM",      Platform="Universal", Popularity="Yes", GCType="Auto",   FileExt=".cs",    Abbr="C#",  Description="Microsoft's answer to Java. Powers .NET, Unity game engine, and Windows desktop apps." },
        new() { Name="Go",         Year=2009, Paradigm="Procedural", Typing="Static",  Compilation="Native",  Platform="System",    Popularity="Yes", GCType="Auto",   FileExt=".go",    Abbr="GO",  Description="Google's minimalist language. Blazing compilation speed. Designed for cloud and server software." },
        new() { Name="Rust",       Year=2015, Paradigm="Multi",      Typing="Static",  Compilation="Native",  Platform="System",    Popularity="Yes", GCType="Manual", FileExt=".rs",    Abbr="RS",  Description="Memory-safe without a garbage collector. Stack Overflow's most loved language 8 years in a row." },
        new() { Name="Kotlin",     Year=2011, Paradigm="Multi",      Typing="Static",  Compilation="VM",      Platform="Mobile",    Popularity="Yes", GCType="Auto",   FileExt=".kt",    Abbr="KT",  Description="JetBrains' modern JVM language. Google's preferred language for Android development." },
        new() { Name="Swift",      Year=2014, Paradigm="OOP",        Typing="Static",  Compilation="Native",  Platform="Mobile",    Popularity="Yes", GCType="Auto",   FileExt=".swift", Abbr="SW",  Description="Apple's replacement for Objective-C. Designed for safety with optional types." },
        new() { Name="Ruby",       Year=1995, Paradigm="OOP",        Typing="Dynamic", Compilation="Script",  Platform="Web",       Popularity="Yes", GCType="Auto",   FileExt=".rb",    Abbr="RB",  Description="Designed for developer happiness. Creator of the Rails web framework." },
        new() { Name="PHP",        Year=1994, Paradigm="Multi",      Typing="Dynamic", Compilation="Script",  Platform="Web",       Popularity="Yes", GCType="Auto",   FileExt=".php",   Abbr="PH",  Description="Powers over 70% of the web including WordPress. The language that built the early internet." },
    };

    private async Task InitAsync()
    {
        if (_db != null) return;
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "langguess.db3");
        _db = new SQLiteAsyncConnection(dbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        int savedVersion = Preferences.Get("db_version", 0);
        if (savedVersion < DbVersion)
        {
            await _db.DropTableAsync<ProgrammingLanguage>();
            Preferences.Set("db_version", DbVersion);
        }

        await _db.CreateTableAsync<ProgrammingLanguage>();
        await _db.CreateTableAsync<GameHistory>();

        if (await _db.Table<ProgrammingLanguage>().CountAsync() == 0)
            await _db.InsertAllAsync(SeedLanguages);
    }

    public async Task<List<ProgrammingLanguage>> GetAllLanguagesAsync()
    {
        await InitAsync();
        return await _db!.Table<ProgrammingLanguage>().OrderBy(l => l.Name).ToListAsync();
    }

    public async Task<ProgrammingLanguage?> GetLanguageByNameAsync(string name)
    {
        await InitAsync();
        return await _db!.Table<ProgrammingLanguage>().FirstOrDefaultAsync(l => l.Name == name);
    }

    public async Task<GameHistory?> GetTodayHistoryAsync()
    {
        await InitAsync();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        return await _db!.Table<GameHistory>().FirstOrDefaultAsync(h => h.Date == today);
    }

    public async Task SaveHistoryAsync(GameHistory history)
    {
        await InitAsync();
        await _db!.InsertAsync(history);
    }

    public async Task<List<GameHistory>> GetAllHistoryAsync()
    {
        await InitAsync();
        return await _db!.Table<GameHistory>().OrderByDescending(h => h.Date).ToListAsync();
    }

    public async Task ClearAllHistoryAsync()
    {
        await InitAsync();
        await _db!.DeleteAllAsync<GameHistory>();
    }

    public async Task<int> GetStreakAsync()
    {
        await InitAsync();
        var history = await _db!.Table<GameHistory>()
            .OrderByDescending(h => h.Date).ToListAsync();

        int streak = 0;
        var date = DateTime.Today;
        foreach (var h in history)
        {
            if (h.Date == date.ToString("yyyy-MM-dd") && h.IsWon)
            {
                streak++;
                date = date.AddDays(-1);
            }
            else break;
        }
        return streak;
    }
}
