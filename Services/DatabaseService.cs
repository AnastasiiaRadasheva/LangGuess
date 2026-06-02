using LangGuess.Models;
using SQLite;

namespace LangGuess.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    private const int DbVersion = 2;

    private static readonly List<ProgrammingLanguage> SeedLanguages = new()
    {
        new() { Name="Python",      Year=1991, Paradigm="Multi",      Typing="Dynamic", Compilation="Interpreted", Platform="Universal", Popularity="Top5",  GCType="GC",     FileExt=".py",   Abbr="PY"  },
        new() { Name="Java",        Year=1995, Paradigm="OOP",        Typing="Static",  Compilation="JVM",         Platform="Universal", Popularity="Top5",  GCType="GC",     FileExt=".java", Abbr="JV"  },
        new() { Name="C",           Year=1972, Paradigm="Procedural", Typing="Static",  Compilation="Compiled",    Platform="System",    Popularity="Top5",  GCType="Manual", FileExt=".c",    Abbr="C"   },
        new() { Name="C++",         Year=1985, Paradigm="Multi",      Typing="Static",  Compilation="Compiled",    Platform="System",    Popularity="Top5",  GCType="Manual", FileExt=".cpp",  Abbr="C++" },
        new() { Name="JavaScript",  Year=1995, Paradigm="Multi",      Typing="Dynamic", Compilation="Interpreted", Platform="Web",       Popularity="Top5",  GCType="GC",     FileExt=".js",   Abbr="JS"  },
        new() { Name="TypeScript",  Year=2012, Paradigm="Multi",      Typing="Static",  Compilation="Transpiled",  Platform="Web",       Popularity="Top20", GCType="GC",     FileExt=".ts",   Abbr="TS"  },
        new() { Name="C#",          Year=2000, Paradigm="Multi",      Typing="Static",  Compilation="Compiled",    Platform="Universal", Popularity="Top5",  GCType="GC",     FileExt=".cs",   Abbr="C#"  },
        new() { Name="Go",          Year=2009, Paradigm="Procedural", Typing="Static",  Compilation="Compiled",    Platform="System",    Popularity="Top20", GCType="GC",     FileExt=".go",   Abbr="GO"  },
        new() { Name="Rust",        Year=2015, Paradigm="Multi",      Typing="Static",  Compilation="Compiled",    Platform="System",    Popularity="Top20", GCType="Own",    FileExt=".rs",   Abbr="RS"  },
        new() { Name="Kotlin",      Year=2011, Paradigm="Multi",      Typing="Static",  Compilation="JVM",         Platform="Mobile",    Popularity="Top20", GCType="GC",     FileExt=".kt",   Abbr="KT"  },
        new() { Name="Swift",       Year=2014, Paradigm="OOP",        Typing="Static",  Compilation="Compiled",    Platform="Mobile",    Popularity="Top20", GCType="ARC",    FileExt=".swift",Abbr="SW"  },
        new() { Name="Ruby",        Year=1995, Paradigm="OOP",        Typing="Dynamic", Compilation="Interpreted", Platform="Web",       Popularity="Top20", GCType="GC",     FileExt=".rb",   Abbr="RB"  },
        new() { Name="PHP",         Year=1994, Paradigm="Multi",      Typing="Dynamic", Compilation="Interpreted", Platform="Web",       Popularity="Top20", GCType="GC",     FileExt=".php",  Abbr="PH"  },
        new() { Name="Scala",       Year=2004, Paradigm="Multi",      Typing="Static",  Compilation="JVM",         Platform="Universal", Popularity="Top20", GCType="GC",     FileExt=".scala",Abbr="SC"  },
        new() { Name="R",           Year=1993, Paradigm="Functional", Typing="Dynamic", Compilation="Interpreted", Platform="Universal", Popularity="Niche", GCType="GC",     FileExt=".r",    Abbr="R"   },
        new() { Name="Perl",        Year=1987, Paradigm="Multi",      Typing="Dynamic", Compilation="Interpreted", Platform="System",    Popularity="Niche", GCType="GC",     FileExt=".pl",   Abbr="PL"  },
        new() { Name="Haskell",     Year=1990, Paradigm="Functional", Typing="Static",  Compilation="Compiled",    Platform="Universal", Popularity="Niche", GCType="GC",     FileExt=".hs",   Abbr="HS"  },
        new() { Name="Lua",         Year=1993, Paradigm="Multi",      Typing="Dynamic", Compilation="Interpreted", Platform="Universal", Popularity="Niche", GCType="GC",     FileExt=".lua",  Abbr="LU"  },
        new() { Name="Dart",        Year=2011, Paradigm="OOP",        Typing="Static",  Compilation="Compiled",    Platform="Mobile",    Popularity="Top20", GCType="GC",     FileExt=".dart", Abbr="DT"  },
        new() { Name="Elixir",      Year=2012, Paradigm="Functional", Typing="Dynamic", Compilation="JVM",         Platform="Web",       Popularity="Niche", GCType="GC",     FileExt=".ex",   Abbr="EX"  },
        new() { Name="F#",          Year=2005, Paradigm="Functional", Typing="Static",  Compilation="Compiled",    Platform="Universal", Popularity="Niche", GCType="GC",     FileExt=".fs",   Abbr="FS"  },
        new() { Name="Clojure",     Year=2007, Paradigm="Functional", Typing="Dynamic", Compilation="JVM",         Platform="Universal", Popularity="Niche", GCType="GC",     FileExt=".clj",  Abbr="CJ"  },
        new() { Name="Bash",        Year=1989, Paradigm="Procedural", Typing="Dynamic", Compilation="Interpreted", Platform="System",    Popularity="Niche", GCType="GC",     FileExt=".sh",   Abbr="BS"  },
        new() { Name="PowerShell",  Year=2006, Paradigm="Multi",      Typing="Dynamic", Compilation="Interpreted", Platform="System",    Popularity="Niche", GCType="GC",     FileExt=".ps1",  Abbr="PS"  },
        new() { Name="Objective-C", Year=1984, Paradigm="OOP",        Typing="Static",  Compilation="Compiled",    Platform="Mobile",    Popularity="Niche", GCType="ARC",    FileExt=".m",    Abbr="OC"  },
        new() { Name="Groovy",      Year=2003, Paradigm="Multi",      Typing="Dynamic", Compilation="JVM",         Platform="Universal", Popularity="Niche", GCType="GC",     FileExt=".groovy",Abbr="GR" },
        new() { Name="Julia",       Year=2012, Paradigm="Multi",      Typing="Dynamic", Compilation="Compiled",    Platform="Universal", Popularity="Niche", GCType="GC",     FileExt=".jl",   Abbr="JL"  },
        new() { Name="MATLAB",      Year=1984, Paradigm="Multi",      Typing="Dynamic", Compilation="Interpreted", Platform="Universal", Popularity="Niche", GCType="GC",     FileExt=".m",    Abbr="ML"  },
        new() { Name="Fortran",     Year=1957, Paradigm="Procedural", Typing="Static",  Compilation="Compiled",    Platform="System",    Popularity="Niche", GCType="Manual", FileExt=".f90",  Abbr="FO"  },
        new() { Name="COBOL",       Year=1959, Paradigm="Procedural", Typing="Static",  Compilation="Compiled",    Platform="System",    Popularity="Niche", GCType="Manual", FileExt=".cbl",  Abbr="CB"  },
    };

    private async Task InitAsync()
    {
        if (_db != null) return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "langguess.db3");
        _db = new SQLiteAsyncConnection(dbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        // Migrate schema when DbVersion increases
        int savedVersion = Preferences.Get("db_version", 0);
        if (savedVersion < DbVersion)
        {
            await _db.DropTableAsync<ProgrammingLanguage>();
            Preferences.Set("db_version", DbVersion);
        }

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
