using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace LangGuess.Services;

public class LocalizationService : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public static LocalizationService Instance { get; } = new();

    private static readonly ResourceManager _rm = new(
        "LangGuess.Resources.Localization.AppResources",
        typeof(LocalizationService).Assembly);

    private LocalizationService() { }

    public string this[string key]
        => _rm.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]";

    public void SetLanguage(string cultureName)
    {
        var ci = new CultureInfo(cultureName);
        CultureInfo.CurrentUICulture = ci;
        CultureInfo.CurrentCulture   = ci;
        // null propertyName = all bindings refresh
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}
