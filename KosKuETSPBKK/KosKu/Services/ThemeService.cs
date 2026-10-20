using System.IO;
using System.Windows;

namespace KosKu.Services;

/// <summary>Manages the application light/dark palette and persists the user's choice.</summary>
public static class ThemeService
{
    private static readonly string ThemeFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KosKu", "theme.txt");

    public static bool IsDark { get; private set; }
    public static string ToggleLabel => IsDark ? "☀ Light mode" : "☾ Dark mode";

    public static void Initialize()
    {
        var dark = false;
        try
        {
            if (File.Exists(ThemeFile))
                dark = string.Equals(File.ReadAllText(ThemeFile).Trim(), "dark", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // If the preference cannot be read, safely fall back to light mode.
        }

        ApplyTheme(dark, persist: false);
    }

    public static void Toggle() => ApplyTheme(!IsDark, persist: true);

    public static void SetDark(bool dark) => ApplyTheme(dark, persist: true);

    private static void ApplyTheme(bool dark, bool persist)
    {
        IsDark = dark;
        var application = Application.Current;
        if (application is not null)
        {
            var dictionaries = application.Resources.MergedDictionaries;
            dictionaries.Clear();
            dictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/KosKu;component/Styles/{(dark ? "DarkColors.xaml" : "Colors.xaml")}", UriKind.Absolute)
            });
            // Reload styles after the palette so StaticResource values in styles use the selected theme.
            dictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/KosKu;component/Styles/Styles.xaml", UriKind.Absolute)
            });
        }

        if (persist)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ThemeFile)!);
                File.WriteAllText(ThemeFile, dark ? "dark" : "light");
            }
            catch
            {
                // Theme still changes for this session even if the preference cannot be saved.
            }
        }
    }
}
