using YoutubeDLSharp;

namespace PandaMultimediaDownloader.Services;

public sealed record ToolPaths(string YtDlp, string Ffmpeg);

/// <summary>Locates or downloads yt-dlp and ffmpeg into <c>tools/</c> next to the executable.</summary>
public sealed class ToolManager
{
    public static readonly string ToolsDir = Path.Combine(AppContext.BaseDirectory, "tools");

    private static readonly string YtDlpPath = Path.Combine(ToolsDir, "yt-dlp.exe");
    private static readonly string FfmpegPath = Path.Combine(ToolsDir, "ffmpeg.exe");
    private static readonly string DenoPath = Path.Combine(ToolsDir, "deno.exe");

    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<ToolPaths> EnsureAsync(IProgress<string>? downloading, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(ToolsDir);

            if (!File.Exists(YtDlpPath))
            {
                downloading?.Report("yt-dlp");
                await Utils.DownloadYtDlp(ToolsDir);
            }

            ct.ThrowIfCancellationRequested();

            var ffmpeg = File.Exists(FfmpegPath) ? FfmpegPath : FindOnPath("ffmpeg.exe");
            if (ffmpeg is null)
            {
                downloading?.Report("ffmpeg");
                await Utils.DownloadFFmpeg(ToolsDir);
                ffmpeg = FfmpegPath;
            }

            ct.ThrowIfCancellationRequested();

            // yt-dlp needs a JS runtime for YouTube's signature challenges; optional, best effort.
            if (!File.Exists(DenoPath) && FindOnPath("deno.exe") is null)
            {
                try
                {
                    downloading?.Report("deno");
                    await Utils.DownloadDeno(ToolsDir);
                }
                catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
                {
                    // Not fatal: many sites work without it.
                }
            }

            if (!File.Exists(YtDlpPath))
            {
                throw new FileNotFoundException("yt-dlp", YtDlpPath);
            }

            if (!File.Exists(ffmpeg))
            {
                throw new FileNotFoundException("ffmpeg", ffmpeg);
            }

            return new ToolPaths(YtDlpPath, ffmpeg);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Re-downloads yt-dlp (sites change often; a stale yt-dlp is the most common failure cause).</summary>
    public async Task<ToolPaths> UpdateAsync(IProgress<string>? downloading, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (File.Exists(YtDlpPath))
            {
                File.Delete(YtDlpPath);
            }
        }
        finally
        {
            _gate.Release();
        }

        return await EnsureAsync(downloading, ct);
    }

    private static string? FindOnPath(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(dir => Path.Combine(dir.Trim(), fileName))
            .FirstOrDefault(File.Exists);
    }
}
