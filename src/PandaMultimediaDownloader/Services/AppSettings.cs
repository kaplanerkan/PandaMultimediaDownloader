using System.Text.Json;
using PandaMultimediaDownloader.Localization;

namespace PandaMultimediaDownloader.Services;

/// <summary>Persisted user settings (settings.json next to the executable).</summary>
public sealed class AppSettings
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Browser names accepted by yt-dlp <c>--cookies-from-browser</c>. Empty = disabled.</summary>
    public static IReadOnlyList<string> SupportedBrowsers { get; } =
        ["", "chrome", "edge", "firefox", "brave", "opera", "vivaldi"];

    public string Language { get; set; } = Loc.DefaultLanguage;

    public string CookiesFromBrowser { get; set; } = string.Empty;

    public bool Mp4 { get; set; } = true;

    public bool Mp3 { get; set; }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (loaded is not null)
                {
                    if (!Loc.IsSupported(loaded.Language))
                    {
                        loaded.Language = Loc.DefaultLanguage;
                    }

                    if (!SupportedBrowsers.Contains(loaded.CookiesFromBrowser))
                    {
                        loaded.CookiesFromBrowser = string.Empty;
                    }

                    return loaded;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupt or unreadable settings: fall back to defaults.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Settings are a convenience; never crash the app over them.
        }
    }
}
