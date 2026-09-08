using System.Text.RegularExpressions;

namespace PandaMultimediaDownloader.Services;

/// <summary>Derives safe output file names from a media title or, as fallback, from the URL.</summary>
public static partial class FileNameHelper
{
    public const int MaxLength = 120;
    private const string Fallback = "media";

    /// <summary>Title if usable, otherwise a name derived from the URL (e.g. <c>youtube_dQw4w9WgXcQ</c>).</summary>
    public static string BuildBaseName(string? title, string url)
    {
        var sanitized = Sanitize(title);
        return sanitized == Fallback ? FromUrl(url) : sanitized;
    }

    /// <summary>Builds <c>{provider}_{id}</c> from the URL. Never throws.</summary>
    public static string FromUrl(string? url)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri))
        {
            return Fallback;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal) || host.StartsWith("m.", StringComparison.Ordinal))
        {
            host = host[(host.IndexOf('.') + 1)..];
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();
        var query = ParseQuery(uri.Query);

        string provider;
        string? id;

        if (host is "youtube.com" or "youtu.be" or "music.youtube.com")
        {
            provider = "youtube";
            id = query.GetValueOrDefault("v")
                 ?? SegmentAfter(segments, "shorts", "embed", "live", "v")
                 ?? (host == "youtu.be" ? segments.FirstOrDefault() : null);
        }
        else if (host.EndsWith("instagram.com", StringComparison.Ordinal))
        {
            provider = "instagram";
            id = SegmentAfter(segments, "p", "reel", "reels", "tv");
        }
        else if (host.EndsWith("facebook.com", StringComparison.Ordinal) || host == "fb.watch")
        {
            provider = "facebook";
            id = query.GetValueOrDefault("v")
                 ?? SegmentAfter(segments, "videos", "reel", "watch")
                 ?? segments.LastOrDefault();
        }
        else
        {
            provider = host.Split('.').FirstOrDefault(p => p.Length > 0) ?? "web";
            id = segments.LastOrDefault();
        }

        id ??= segments.LastOrDefault();
        var idPart = Sanitize(id, 60);
        return idPart == Fallback ? provider : $"{provider}_{idPart}";
    }

    /// <summary>Strips characters invalid for Windows file names and yt-dlp templates.</summary>
    public static string Sanitize(string? name, int maxLength = MaxLength)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Fallback;
        }

        var invalid = Path.GetInvalidFileNameChars().Concat(['%']).ToHashSet();
        var chars = name.Trim().Select(c => invalid.Contains(c) || char.IsControl(c) ? '_' : c);
        var result = WhitespaceRegex().Replace(new string(chars.ToArray()), " ").Trim(' ', '.', '_');

        if (result.Length > maxLength)
        {
            result = result[..maxLength].TrimEnd(' ', '.', '_');
        }

        return result.Length == 0 || IsReservedName(result) ? Fallback : result;
    }

    /// <summary>Returns <c>dir/base.ext</c>, appending <c>_2</c>, <c>_3</c>... when the file exists.</summary>
    public static string UniquePath(string directory, string baseName, string extension)
    {
        var candidate = Path.Combine(directory, baseName + extension);
        for (var i = 2; File.Exists(candidate); i++)
        {
            candidate = Path.Combine(directory, $"{baseName}_{i}{extension}");
        }

        return candidate;
    }

    private static string? SegmentAfter(string[] segments, params string[] markers)
    {
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (markers.Contains(segments[i], StringComparer.OrdinalIgnoreCase))
            {
                return segments[i + 1];
            }
        }

        return null;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            var key = eq < 0 ? pair : pair[..eq];
            var value = eq < 0 ? string.Empty : Uri.UnescapeDataString(pair[(eq + 1)..]);
            result.TryAdd(key, value);
        }

        return result;
    }

    private static bool IsReservedName(string name)
    {
        var stem = name.Split('.')[0].ToUpperInvariant();
        return stem is "CON" or "PRN" or "AUX" or "NUL"
            || (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && char.IsDigit(stem[3]));
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
