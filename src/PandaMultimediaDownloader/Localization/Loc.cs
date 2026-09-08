using System.ComponentModel;

namespace PandaMultimediaDownloader.Localization;

/// <summary>
/// Runtime-switchable localization. Bind in XAML with
/// <c>{Binding [Key], Source={x:Static loc:Loc.Instance}}</c>.
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    public const string DefaultLanguage = "tr";

    public static IReadOnlyList<LanguageItem> Languages { get; } =
    [
        new("tr", "Türkçe"),
        new("de", "Deutsch"),
        new("en", "English"),
    ];

    private string _language = DefaultLanguage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language
    {
        get => _language;
        set
        {
            if (!Strings.ContainsKey(value) || value == _language)
            {
                return;
            }

            _language = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }

    public string this[string key] => Lookup(_language, key);

    public string Format(string key, params object[] args) =>
        args.Length == 0 ? this[key] : string.Format(this[key], args);

    public static bool IsSupported(string? code) => code is not null && Strings.ContainsKey(code);

    private static string Lookup(string lang, string key)
    {
        if (Strings[lang].TryGetValue(key, out var value))
        {
            return value;
        }

        return Strings["en"].TryGetValue(key, out var fallback) ? fallback : key;
    }

    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        ["tr"] = new()
        {
            ["AppTitle"] = "Panda Multimedia Downloader",
            ["Subtitle"] = "YouTube, Instagram, Facebook ve daha fazlası için MP4 / MP3 indirici",
            ["UrlLabel"] = "Medya adresi (URL)",
            ["UrlPlaceholder"] = "https://www.youtube.com/watch?v=...",
            ["Paste"] = "Yapıştır",
            ["FormatLabel"] = "Çıktı formatı",
            ["Mp4"] = "MP4 (video)",
            ["Mp3"] = "MP3 (ses)",
            ["Download"] = "İndir",
            ["Cancel"] = "İptal",
            ["OpenFolder"] = "Klasörü aç",
            ["UpdateTools"] = "Araçları güncelle",
            ["ClearLog"] = "Günlüğü temizle",
            ["LanguageLabel"] = "Dil",
            ["OutputFolder"] = "Kayıt klasörü",
            ["LogLabel"] = "Günlük",
            ["StatusReady"] = "Hazır",
            ["StatusPreparingTools"] = "Araçlar hazırlanıyor (yt-dlp / ffmpeg)...",
            ["StatusFetchingInfo"] = "Medya bilgisi alınıyor...",
            ["StatusDownloading"] = "İndiriliyor {0}: %{1:0}",
            ["StatusConverting"] = "Dönüştürülüyor ({0})...",
            ["StatusDone"] = "Tamamlandı: {0} dosya",
            ["StatusCancelled"] = "İptal edildi",
            ["StatusError"] = "Hata: {0}",
            ["ErrInvalidUrl"] = "Geçerli bir http/https adresi girin.",
            ["ErrNoFormat"] = "En az bir format seçin (MP4 veya MP3).",
            ["ErrDownloadFailed"] = "İndirme başarısız oldu.",
            ["ErrToolMissing"] = "Gerekli araç bulunamadı: {0}",
            ["LogToolsReady"] = "Araçlar hazır: {0}",
            ["LogToolDownloading"] = "İndiriliyor: {0}",
            ["LogTitle"] = "Başlık: {0}",
            ["LogFileName"] = "Dosya adı: {0}",
            ["LogSaved"] = "Kaydedildi: {0}",
            ["LogHintCookies"] = "İpucu: Instagram/Facebook için oturum gerekebilir. 'Tarayıcı çerezlerini kullan' seçeneğini deneyin.",
            ["CookiesLabel"] = "Tarayıcı çerezlerini kullan",
            ["CookiesNone"] = "Kapalı",
            ["Speed"] = "Hız",
            ["Eta"] = "Kalan",
            ["ToolsUpdated"] = "Araçlar güncellendi.",
        },
        ["de"] = new()
        {
            ["AppTitle"] = "Panda Multimedia Downloader",
            ["Subtitle"] = "MP4 / MP3 Downloader für YouTube, Instagram, Facebook und mehr",
            ["UrlLabel"] = "Medienadresse (URL)",
            ["UrlPlaceholder"] = "https://www.youtube.com/watch?v=...",
            ["Paste"] = "Einfügen",
            ["FormatLabel"] = "Ausgabeformat",
            ["Mp4"] = "MP4 (Video)",
            ["Mp3"] = "MP3 (Audio)",
            ["Download"] = "Herunterladen",
            ["Cancel"] = "Abbrechen",
            ["OpenFolder"] = "Ordner öffnen",
            ["UpdateTools"] = "Tools aktualisieren",
            ["ClearLog"] = "Protokoll leeren",
            ["LanguageLabel"] = "Sprache",
            ["OutputFolder"] = "Zielordner",
            ["LogLabel"] = "Protokoll",
            ["StatusReady"] = "Bereit",
            ["StatusPreparingTools"] = "Tools werden vorbereitet (yt-dlp / ffmpeg)...",
            ["StatusFetchingInfo"] = "Medieninformationen werden abgerufen...",
            ["StatusDownloading"] = "Download {0}: {1:0} %",
            ["StatusConverting"] = "Konvertierung ({0})...",
            ["StatusDone"] = "Fertig: {0} Datei(en)",
            ["StatusCancelled"] = "Abgebrochen",
            ["StatusError"] = "Fehler: {0}",
            ["ErrInvalidUrl"] = "Bitte eine gültige http/https-Adresse eingeben.",
            ["ErrNoFormat"] = "Mindestens ein Format wählen (MP4 oder MP3).",
            ["ErrDownloadFailed"] = "Download fehlgeschlagen.",
            ["ErrToolMissing"] = "Benötigtes Tool nicht gefunden: {0}",
            ["LogToolsReady"] = "Tools bereit: {0}",
            ["LogToolDownloading"] = "Wird heruntergeladen: {0}",
            ["LogTitle"] = "Titel: {0}",
            ["LogFileName"] = "Dateiname: {0}",
            ["LogSaved"] = "Gespeichert: {0}",
            ["LogHintCookies"] = "Hinweis: Instagram/Facebook benötigen evtl. eine Anmeldung. Option 'Browser-Cookies verwenden' probieren.",
            ["CookiesLabel"] = "Browser-Cookies verwenden",
            ["CookiesNone"] = "Aus",
            ["Speed"] = "Geschw.",
            ["Eta"] = "Rest",
            ["ToolsUpdated"] = "Tools aktualisiert.",
        },
        ["en"] = new()
        {
            ["AppTitle"] = "Panda Multimedia Downloader",
            ["Subtitle"] = "MP4 / MP3 downloader for YouTube, Instagram, Facebook and more",
            ["UrlLabel"] = "Media address (URL)",
            ["UrlPlaceholder"] = "https://www.youtube.com/watch?v=...",
            ["Paste"] = "Paste",
            ["FormatLabel"] = "Output format",
            ["Mp4"] = "MP4 (video)",
            ["Mp3"] = "MP3 (audio)",
            ["Download"] = "Download",
            ["Cancel"] = "Cancel",
            ["OpenFolder"] = "Open folder",
            ["UpdateTools"] = "Update tools",
            ["ClearLog"] = "Clear log",
            ["LanguageLabel"] = "Language",
            ["OutputFolder"] = "Output folder",
            ["LogLabel"] = "Log",
            ["StatusReady"] = "Ready",
            ["StatusPreparingTools"] = "Preparing tools (yt-dlp / ffmpeg)...",
            ["StatusFetchingInfo"] = "Fetching media info...",
            ["StatusDownloading"] = "Downloading {0}: {1:0}%",
            ["StatusConverting"] = "Converting ({0})...",
            ["StatusDone"] = "Done: {0} file(s)",
            ["StatusCancelled"] = "Cancelled",
            ["StatusError"] = "Error: {0}",
            ["ErrInvalidUrl"] = "Enter a valid http/https address.",
            ["ErrNoFormat"] = "Select at least one format (MP4 or MP3).",
            ["ErrDownloadFailed"] = "Download failed.",
            ["ErrToolMissing"] = "Required tool not found: {0}",
            ["LogToolsReady"] = "Tools ready: {0}",
            ["LogToolDownloading"] = "Downloading: {0}",
            ["LogTitle"] = "Title: {0}",
            ["LogFileName"] = "File name: {0}",
            ["LogSaved"] = "Saved: {0}",
            ["LogHintCookies"] = "Hint: Instagram/Facebook may require a login. Try the 'Use browser cookies' option.",
            ["CookiesLabel"] = "Use browser cookies",
            ["CookiesNone"] = "Off",
            ["Speed"] = "Speed",
            ["Eta"] = "ETA",
            ["ToolsUpdated"] = "Tools updated.",
        },
    };
}

public sealed record LanguageItem(string Code, string Name);
