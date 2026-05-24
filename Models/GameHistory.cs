using SQLite;

namespace LangGuess.Models;

[Table("GameHistory")]
public class GameHistory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Date { get; set; } = "";          // yyyy-MM-dd

    [NotNull]
    public string SecretLanguage { get; set; } = "";

    public int Attempts { get; set; }

    public bool IsWon { get; set; }

    public string GuessedLanguages { get; set; } = ""; // comma-separated names
}
