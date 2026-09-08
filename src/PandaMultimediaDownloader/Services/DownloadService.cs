using System.Diagnostics;
using System.Text.RegularExpressions;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace PandaMultimediaDownloader.Services;

public enum DownloadPhase
{
    PreparingTools,
    FetchingInfo,
    Downloading,
    Converting,
}

public sealed record DownloadStatus(
    DownloadPhase Phase,
    string Target,
    double Percent,
    string? Speed = null,
    string? Eta = null);

public sealed record DownloadRequest(
    string Url,
    bool Mp4,
    bool Mp3,
    string OutputDir,
    string CookiesFromBrowser = "");

public sealed record DownloadResult(string BaseName, string? Title, IReadOnlyList<string> Files);

/// <summary>Downloads a media URL as MP4 and/or MP3 via yt-dlp + ffmpeg.</summary>
public sealed partial class DownloadService(ToolManager tools)
{
    private const string VideoFormat =
        "bestvideo[ext=mp4]+bestaudio[ext=m4a]/bestvideo+bestaudio/best";

    /// <summary>Matches yt-dlp per-stream intermediates such as <c>name.f137.mp4</c> or <c>name.fdash-123a.m4a</c>.</summary>
    [GeneratedRegex(@"\.f[\w-]+\.(mp4|m4a|webm|mkv|opus|aac)$", RegexOptions.IgnoreCase)]
    private static partial Regex FragmentRegex();

    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadStatus> status,
        IProgress<string> log,
        CancellationToken ct)
    {
        status.Report(new DownloadStatus(DownloadPhase.PreparingTools, string.Empty, 0));
        var paths = await tools.EnsureAsync(new Progress<string>(log.Report), ct);
        Directory.CreateDirectory(request.OutputDir);

        var ydl = new YoutubeDL
        {
            YoutubeDLPath = paths.YtDlp,
            FFmpegPath = paths.Ffmpeg,
            OutputFolder = request.OutputDir,
            OverwriteFiles = false,
            RestrictFilenames = false,
        };

        var options = new OptionSet { NoPlaylist = true, NoMtime = true };
        if (!string.IsNullOrWhiteSpace(request.CookiesFromBrowser))
        {
            options.CookiesFromBrowser = request.CookiesFromBrowser;
        }

        status.Report(new DownloadStatus(DownloadPhase.FetchingInfo, string.Empty, 0));
        var title = await FetchTitleAsync(ydl, request.Url, options, ct);
        var baseName = FileNameHelper.BuildBaseName(title, request.Url);

        var files = new List<string>();
        string? mp4Path = null;

        try
        {
            if (request.Mp4)
            {
                mp4Path = await DownloadVideoAsync(ydl, request, options, baseName, status, log, ct);
                files.Add(mp4Path);
            }

            if (request.Mp3)
            {
                var mp3Path = mp4Path is null
                    ? await DownloadAudioAsync(ydl, request, options, baseName, status, log, ct)
                    : await ExtractMp3Async(paths.Ffmpeg, mp4Path, baseName, request.OutputDir, status, ct);
                files.Add(mp3Path);
            }
        }
        finally
        {
            await CleanupPartialFilesAsync(request.OutputDir, baseName, files);
        }

        return new DownloadResult(baseName, title, files);
    }

    /// <summary>
    /// Removes yt-dlp leftovers (.part, .ytdl, .fNNN.* fragments) after cancel or failure.
    /// A killed yt-dlp may still hold handles for a moment, so deletion is retried briefly.
    /// </summary>
    private static async Task CleanupPartialFilesAsync(string dir, string baseName, IReadOnlyCollection<string> keep)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var leftovers = Directory.EnumerateFiles(dir, baseName + "*")
                .Where(f => !keep.Contains(f, StringComparer.OrdinalIgnoreCase))
                .Where(f => IsTemporary(Path.GetFileName(f)))
                .ToList();

            if (leftovers.Count == 0)
            {
                return;
            }

            foreach (var file in leftovers)
            {
                TryDelete(file);
            }

            await Task.Delay(200);
        }
    }

    private static bool IsTemporary(string name) =>
        name.EndsWith(".part", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase)
        || name.Contains(".part-Frag", StringComparison.OrdinalIgnoreCase)
        || FragmentRegex().IsMatch(name);

    private static async Task<string?> FetchTitleAsync(YoutubeDL ydl, string url, OptionSet options, CancellationToken ct)
    {
        try
        {
            var info = await ydl.RunVideoDataFetch(url, ct, overrideOptions: options);
            return info.Success ? info.Data?.Title : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private static async Task<string> DownloadVideoAsync(
        YoutubeDL ydl,
        DownloadRequest request,
        OptionSet options,
        string baseName,
        IProgress<DownloadStatus> status,
        IProgress<string> log,
        CancellationToken ct)
    {
        var target = FileNameHelper.UniquePath(request.OutputDir, baseName, ".mp4");
        ydl.OutputFileTemplate = Path.GetFileNameWithoutExtension(target) + ".%(ext)s";

        var result = await ydl.RunVideoDownload(
            request.Url,
            VideoFormat,
            DownloadMergeFormat.Mp4,
            VideoRecodeFormat.None,
            ct,
            ProgressFor("MP4", status),
            log,
            options);

        return ResolveOutput(result, target, ".mp4", ct);
    }

    private static async Task<string> DownloadAudioAsync(
        YoutubeDL ydl,
        DownloadRequest request,
        OptionSet options,
        string baseName,
        IProgress<DownloadStatus> status,
        IProgress<string> log,
        CancellationToken ct)
    {
        var target = FileNameHelper.UniquePath(request.OutputDir, baseName, ".mp3");
        ydl.OutputFileTemplate = Path.GetFileNameWithoutExtension(target) + ".%(ext)s";

        var result = await ydl.RunAudioDownload(
            request.Url,
            AudioConversionFormat.Mp3,
            ct,
            ProgressFor("MP3", status),
            log,
            options);

        return ResolveOutput(result, target, ".mp3", ct);
    }

    /// <summary>Re-encodes the audio track of an already downloaded MP4 to MP3 (avoids a second network download).</summary>
    private static async Task<string> ExtractMp3Async(
        string ffmpeg,
        string mp4Path,
        string baseName,
        string outputDir,
        IProgress<DownloadStatus> status,
        CancellationToken ct)
    {
        status.Report(new DownloadStatus(DownloadPhase.Converting, "MP3", 0));
        var target = FileNameHelper.UniquePath(outputDir, baseName, ".mp3");

        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        psi.ArgumentList.Add("-y");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(mp4Path);
        psi.ArgumentList.Add("-vn");
        psi.ArgumentList.Add("-codec:a");
        psi.ArgumentList.Add("libmp3lame");
        psi.ArgumentList.Add("-q:a");
        psi.ArgumentList.Add("2");
        psi.ArgumentList.Add(target);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("ffmpeg could not be started.");
        var stderr = process.StandardError.ReadToEndAsync(ct);
        _ = process.StandardOutput.ReadToEndAsync(ct);

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            TryDelete(target);
            throw;
        }

        if (process.ExitCode != 0)
        {
            TryDelete(target);
            var tail = (await stderr).Split('\n', StringSplitOptions.RemoveEmptyEntries).TakeLast(3);
            throw new InvalidOperationException("ffmpeg: " + string.Join(" | ", tail).Trim());
        }

        status.Report(new DownloadStatus(DownloadPhase.Converting, "MP3", 100));
        return target;
    }

    private static IProgress<DownloadProgress> ProgressFor(string target, IProgress<DownloadStatus> status) =>
        new Progress<DownloadProgress>(p =>
        {
            var phase = p.State switch
            {
                DownloadState.PostProcessing => DownloadPhase.Converting,
                _ => DownloadPhase.Downloading,
            };
            var percent = p.State == DownloadState.PostProcessing ? 100 : p.Progress * 100;
            status.Report(new DownloadStatus(phase, target, percent, p.DownloadSpeed, p.ETA));
        });

    private static string ResolveOutput(RunResult<string> result, string expected, string extension, CancellationToken ct)
    {
        // yt-dlp killed by cancel reports a failed run rather than throwing.
        ct.ThrowIfCancellationRequested();

        if (!result.Success)
        {
            var error = result.ErrorOutput?.LastOrDefault(l => l.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
                        ?? result.ErrorOutput?.LastOrDefault()
                        ?? "yt-dlp failed";
            throw new InvalidOperationException(error.Trim());
        }

        if (File.Exists(expected))
        {
            return expected;
        }

        if (!string.IsNullOrEmpty(result.Data) && File.Exists(result.Data))
        {
            return result.Data;
        }

        var stem = Path.GetFileNameWithoutExtension(expected);
        var dir = Path.GetDirectoryName(expected)!;
        var match = Directory.EnumerateFiles(dir, stem + ".*")
            .Where(f => !f.EndsWith(".part", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => string.Equals(Path.GetExtension(f), extension, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        return match ?? throw new FileNotFoundException("Output file not found", expected);
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
