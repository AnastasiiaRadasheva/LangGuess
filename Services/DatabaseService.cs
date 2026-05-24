using LangGuess.Models;
using SQLite;

namespace LangGuess.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    private static readonly List<ProgrammingLanguage> SeedLanguages = new()
    {
        new() { Name="Python",       Year=1991, Paradigm="Multi",       Typing="Dynamic",  Compilation="Interpreted", Platform="Universal", Popularity="Top5",  Abbr="PY"  },
        new() { Name="Java",         Year=1995, Paradigm="OOP",         Typing="Static",   Compilation="JVM",         Platform="Universal", Popularity="Top5",  Abbr="JV"  },
        new() { Name="C",            Year=1972, Paradigm="Procedural",  Typing="Static",   Compilation="Compiled",    Platform="System",    Popularity="Top5",  Abbr="C"   },
        new() { Name="C++",          Year=1985, Paradigm="Multi",       Typing="Static",   Compilation="Compiled",    Platform="System",    Popularity="Top5",  Abbr="C++" },
        new() { Name="JavaScript",   Year=1995, Paradigm="Multi",       Typing="Dynamic",  Compilation="Interpreted", Platform="Web",       Popularity="Top5",  Abbr="JS"  },
        new() { Name="TypeScript",   Year=2012, Paradigm="Multi",       Typing="Static",   Compilation="Transpiled",  Platform="Web",       Popularity="Top20", Abbr="TS"  },
        new() { Name="C#",           Year=2000, Paradigm="Multi",       Typing="Static",   Compilation="Compiled",    Platform="Universal", Popularity="Top5",  Abbr="C#"  },
        new() { Name="Go",           Year=2009, Paradigm="Procedural",  Typing="Static",   Compilation="Compiled",    Platform="System",    Popularity="Top20", Abbr="GO"  },
        new() { Name="Rust",         Year=2015, Paradigm="Multi",       Typing="Static",   Compilation="Compiled",    Platform="System",    Popularity="Top20", Abbr="RS"  },
        new() { Name="Kotlin",       Year=2011, Paradigm="Multi",       Typing="Static",   Compilation="JVM",         Platform="Mobile",    Popularity="Top20", Abbr="KT"  },
        new() { Name="Swift",        Year=2014, Paradigm="OOP",         Typing="Static",   Compilation="Compiled",    Platform="Mobile",    Popularity="Top20", Abbr="SW"  },
        new() { Name="Ruby",         Year=1995, Paradigm="OOP",         Typing="Dynamic",  Compilation="Interpreted", Platform="Web",       Popularity="Top20", Abbr="RB"  },
        new() { Name="PHP",          Year=1994, Paradigm="Multi",       Typing="Dynamic",  Compilation="Interpreted", Platform="Web",       Popularity="Top20", Abbr="PH"  },
        new() { Name="Scala",        Year=2004, Paradigm="Multi",       Typing="Static",   Compilation="JVM",         Platform="Universal", Popularity="Top20", Abbr="SC"  },
        new() { Name="R",            Year=1993, Paradigm="Functional",  Typing="Dynamic",  Compilation="Interpreted", Platform="Universal", Popularity="Niche", Abbr="R"   },
        new() { Name="Perl",         Year=1987, Paradigm="Multi",       Typing="Dynamic",  Compilation="Interpreted", Platform="System",    Popularity="Niche", Abbr="PL"  },
        new() { Name="Haskell",      Year=1990, Paradigm="Functional",  Typing="Static",   Compilation="Compiled",    Platform="Universal", Popularity="Niche", Abbr="HS"  },
        new() { Name="Lua",          Year=1993, Paradigm="Multi",       Typing="Dynamic",  Compilation="Interpreted", Platform="Universal", Popularity="Niche", Abbr="LU"  },
        new() { Name="Dart",         Year=2011, Paradigm="OOP",         Typing="Static",   Compilation="Compiled",    Platform="Mobile",    Popularity="Top20", Abbr="DT"  },
        new() { Name="Elixir",       Year=2012, Paradigm="Functional",  Typing="Dynamic",  Compilation="JVM",         Platform="Web",       Popularity="Niche", Abbr="EX"  },
        new() { Name="F#",           Year=2005, Paradigm="Functional",  Typing="Static",   Compilation="Compiled",    Platform="Universal", Popularity="Niche", Abbr="FS"  },
        new() { Name="Clojure",      Year=2007, Paradigm="Functional",  Typing="Dynamic",  Compilation="JVM",         Platform="Universal", Popularity="Niche", Abbr="CJ"  },
        new() { Name="Bash",         Year=1989, Paradigm="Procedural",  Typing="Dynamic",  Compilation="Interpreted", Platform="System",    Popularity="Niche", Abbr="BS"  },
        new() { Name="PowerShell",   Year=2006, Paradigm="Multi",       Typing="Dynamic",  Compilation="Interpreted", Platform="System",    Popularity="Niche", Abbr="PS"  },
        new() { Name="Objective-C",  Year=1984, Paradigm="OOP",         Typing="Static",   Compilation="Compiled",    Platform="Mobile",    Popularity="Niche", Abbr="OC"  },
    };

    private async Task InitAsync()
    {
        if (_db != null) return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "langguess.db3");
        _db = new SQLiteAsyncConnection(dbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<ProgrammingLanguage>();
        await _db.CreateTableAsync<GameHistory>();

        // Seed languages once
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
        return await _db!.Table<ProgrammingLanguage>()
            .FirstOrDefaultAsync(l => l.Name == name);
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
        return await _db!.Table<GameHistory>()
            .OrderByDescending(h => h.Date).ToListAsync();
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
