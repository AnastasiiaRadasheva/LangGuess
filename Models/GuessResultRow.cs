namespace LangGuess.Models;

public class GuessResultRow
{
    public string LanguageName { get; set; } = "";
    public string Abbr { get; set; } = "";

    public string Year { get; set; } = "";
    public GuessStatus YearStatus { get; set; }

    public string Paradigm { get; set; } = "";
    public GuessStatus ParadigmStatus { get; set; }

    public string Typing { get; set; } = "";
    public GuessStatus TypingStatus { get; set; }

    public string Compilation { get; set; } = "";
    public GuessStatus CompilationStatus { get; set; }

    public string Platform { get; set; } = "";
    public GuessStatus PlatformStatus { get; set; }

    public string Popularity { get; set; } = "";
    public GuessStatus PopularityStatus { get; set; }

    public bool IsWin { get; set; }

    public Color GetColor(GuessStatus s) => s switch
    {
        GuessStatus.Green  => Color.FromArgb("#238636"),
        GuessStatus.Yellow => Color.FromArgb("#9E6A03"),
        GuessStatus.Red    => Color.FromArgb("#8B1B1B"),
        _                  => Color.FromArgb("#1F2430")
    };

    public Color AbbrColor  => Color.FromArgb("#1F2430");
    public Color YearColor       => GetColor(YearStatus);
    public Color ParadigmColor   => GetColor(ParadigmStatus);
    public Color TypingColor     => GetColor(TypingStatus);
    public Color CompilationColor=> GetColor(CompilationStatus);
    public Color PlatformColor   => GetColor(PlatformStatus);
    public Color PopularityColor => GetColor(PopularityStatus);
}
