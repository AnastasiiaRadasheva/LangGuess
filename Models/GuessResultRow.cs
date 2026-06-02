namespace LangGuess.Models;

public class GuessResultRow
{
    public string LanguageName { get; set; } = "";
    public string Abbr { get; set; } = "";

    public string Year { get; set; } = "";
    public GuessStatus YearStatus { get; set; }

    public string Paradigm      { get; set; } = "";
    public GuessStatus ParadigmStatus { get; set; }
    public string ParadigmArrow { get; set; } = "";

    public string Typing        { get; set; } = "";
    public GuessStatus TypingStatus { get; set; }
    public string TypingArrow   { get; set; } = "";

    public string Compilation   { get; set; } = "";
    public GuessStatus CompilationStatus { get; set; }
    public string CompilationArrow { get; set; } = "";

    public string Platform      { get; set; } = "";
    public GuessStatus PlatformStatus { get; set; }
    public string PlatformArrow { get; set; } = "";

    public string Popularity    { get; set; } = "";
    public GuessStatus PopularityStatus { get; set; }
    public string PopArrow      { get; set; } = "";

    public string GCType        { get; set; } = "";
    public GuessStatus GCTypeStatus { get; set; }
    public string GCTypeArrow   { get; set; } = "";

    public string YearArrow     { get; set; } = "";

    public bool IsWin { get; set; }

    // Displayed text with arrow appended for numeric/ordered fields
    public string YearDisplay => YearArrow == "" ? Year : $"{Year} {YearArrow}";
    public string PopDisplay  => PopArrow  == "" ? Popularity : $"{Popularity}\n{PopArrow}";

    public Color GetColor(GuessStatus s) => s switch
    {
        GuessStatus.Green  => Color.FromArgb("#1A6B2A"),
        GuessStatus.Yellow => Color.FromArgb("#7A5100"),
        GuessStatus.Red    => Color.FromArgb("#6B1515"),
        _                  => Color.FromArgb("#161B22")
    };

    public Color ArrowColor(string arrow) => arrow == "↑"
        ? Color.FromArgb("#A374FF")
        : Color.FromArgb("#FF8C42");

    public Color AbbrColor      => Color.FromArgb("#1A1435");
    public Color YearColor       => GetColor(YearStatus);
    public Color ParadigmColor   => GetColor(ParadigmStatus);
    public Color TypingColor     => GetColor(TypingStatus);
    public Color CompilationColor=> GetColor(CompilationStatus);
    public Color PlatformColor   => GetColor(PlatformStatus);
    public Color PopularityColor => GetColor(PopularityStatus);
    public Color GCTypeColor     => GetColor(GCTypeStatus);

    public Color YearArrowColor       => YearArrow       == "" ? Colors.Transparent : ArrowColor(YearArrow);
    public Color PopArrowColor        => PopArrow        == "" ? Colors.Transparent : ArrowColor(PopArrow);
    public Color ParadigmArrowColor   => ParadigmArrow   == "" ? Colors.Transparent : ArrowColor(ParadigmArrow);
    public Color TypingArrowColor     => TypingArrow     == "" ? Colors.Transparent : ArrowColor(TypingArrow);
    public Color CompilationArrowColor=> CompilationArrow== "" ? Colors.Transparent : ArrowColor(CompilationArrow);
    public Color PlatformArrowColor   => PlatformArrow   == "" ? Colors.Transparent : ArrowColor(PlatformArrow);
    public Color GCTypeArrowColor     => GCTypeArrow     == "" ? Colors.Transparent : ArrowColor(GCTypeArrow);
}
