using SQLite;

namespace LangGuess.Models;

[Table("Languages")]
public class ProgrammingLanguage
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = "";

    [NotNull]
    public int Year { get; set; }

    // OOP | Functional | Procedural | Multi
    [NotNull]
    public string Paradigm { get; set; } = "";

    // Static | Dynamic
    [NotNull]
    public string Typing { get; set; } = "";

    // Compiled | Interpreted | JVM | Transpiled
    [NotNull]
    public string Compilation { get; set; } = "";

    // Web | System | Mobile | Universal
    [NotNull]
    public string Platform { get; set; } = "";

    // Top5 | Top20 | Niche
    [NotNull]
    public string Popularity { get; set; } = "";

    // 2-3 char abbreviation used as icon
    public string Abbr { get; set; } = "";
}
