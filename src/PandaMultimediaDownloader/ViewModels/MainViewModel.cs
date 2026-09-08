using System.Diagnostics;
using System.Text;
using System.Windows;
using PandaMultimediaDownloader.Localization;
using PandaMultimediaDownloader.Services;

namespace PandaMultimediaDownloader.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    public static readonly string OutputDir = Path.Combine(AppContext.BaseDirectory, "downloaded");

    private readonly AppSettings _settings;
    private readonly ToolManager _tools = new();
    private readonly DownloadService _downloader;
    private readonly StringBuilder _log = new();

    private CancellationTokenSource? _cts;
    private Func<string> _statusRender = () => Loc.Instance["StatusReady"];

    private string _url = string.Empty;
    private bool _isBusy;
    private double _progress;
    private bool _isIndeterminate;
    private string _speedText = string.Empty;

    public MainViewModel()
    {
        _settings = AppSettings.Load();
        _downloader = new DownloadService(_tools);
        Loc.Instance.Language = _settings.Language;
        Loc.Instance.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(SelectedLanguage));
        };

        DownloadCommand = new AsyncRelayCommand(DownloadAsync, () => !IsBusy);
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsBusy);
        OpenFolderCommand = new RelayCommand(OpenFolder);
        PasteCommand = new RelayCommand(PasteFromClipboard, () => !IsBusy);
        UpdateToolsCommand = new AsyncRelayCommand(UpdateToolsAsync, () => !IsBusy);
        ClearLogCommand = new RelayCommand(ClearLog);
    }

    public IReadOnlyList<LanguageItem> Languages => Loc.Languages;

    public IReadOnlyList<string> Browsers => AppSettings.SupportedBrowsers;

    public string OutputFolder => OutputDir;

    public AsyncRelayCommand DownloadCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand OpenFolderCommand { get; }

    public RelayCommand PasteCommand { get; }

    public AsyncRelayCommand UpdateToolsCommand { get; }

    public RelayCommand ClearLogCommand { get; }

    public string Url
    {
        get => _url;
        set => Set(ref _url, value);
    }

    public bool IsMp4
    {
        get => _settings.Mp4;
        set
        {
            if (_settings.Mp4 != value)
            {
                _settings.Mp4 = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }
    }

    public bool IsMp3
    {
        get => _settings.Mp3;
        set
        {
            if (_settings.Mp3 != value)
            {
                _settings.Mp3 = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }
    }

    public string SelectedLanguage
    {
        get => Loc.Instance.Language;
        set
        {
            if (Loc.IsSupported(value) && value != Loc.Instance.Language)
            {
                Loc.Instance.Language = value;
                _settings.Language = value;
                _settings.Save();
            }
        }
    }

    public string CookiesBrowser
    {
        get => _settings.CookiesFromBrowser;
        set
        {
            if (_settings.CookiesFromBrowser != value)
            {
                _settings.CookiesFromBrowser = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (Set(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsIdle));
                DownloadCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
                PasteCommand.RaiseCanExecuteChanged();
                UpdateToolsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsIdle => !_isBusy;

    public double Progress
    {
        get => _progress;
        private set => Set(ref _progress, value);
    }

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        private set => Set(ref _isIndeterminate, value);
    }

    public string StatusText => _statusRender();

    public string SpeedText
    {
        get => _speedText;
        private set => Set(ref _speedText, value);
    }

    public string LogText => _log.ToString();

    private async Task DownloadAsync()
    {
        var url = Url.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            SetStatus(() => Loc.Instance["ErrInvalidUrl"]);
            return;
        }

        if (!IsMp4 && !IsMp3)
        {
            SetStatus(() => Loc.Instance["ErrNoFormat"]);
            return;
        }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        Progress = 0;
        SpeedText = string.Empty;
        AppendLog($"> {url}");

        try
        {
            var request = new DownloadRequest(url, IsMp4, IsMp3, OutputDir, CookiesBrowser);
            var result = await _downloader.DownloadAsync(
                request,
                new Progress<DownloadStatus>(OnStatus),
                new Progress<string>(OnToolOutput),
                _cts.Token);

            if (result.Title is not null)
            {
                AppendLog(Loc.Instance.Format("LogTitle", result.Title));
            }

            AppendLog(Loc.Instance.Format("LogFileName", result.BaseName));
            foreach (var file in result.Files)
            {
                AppendLog(Loc.Instance.Format("LogSaved", file));
            }

            Progress = 100;
            var count = result.Files.Count;
            SetStatus(() => Loc.Instance.Format("StatusDone", count));
        }
        catch (OperationCanceledException)
        {
            SetStatus(() => Loc.Instance["StatusCancelled"]);
        }
        catch (FileNotFoundException ex) when (ex.Message is "yt-dlp" or "ffmpeg")
        {
            var tool = ex.Message;
            SetStatus(() => Loc.Instance.Format("ErrToolMissing", tool));
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            AppendLog(message);
            SetStatus(() => Loc.Instance.Format("StatusError", message));
            if (uri.Host.Contains("instagram", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Contains("facebook", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Contains("fb.watch", StringComparison.OrdinalIgnoreCase))
            {
                AppendLog(Loc.Instance["LogHintCookies"]);
            }
        }
        finally
        {
            IsIndeterminate = false;
            SpeedText = string.Empty;
            IsBusy = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    private async Task UpdateToolsAsync()
    {
        _cts = new CancellationTokenSource();
        IsBusy = true;
        IsIndeterminate = true;
        SetStatus(() => Loc.Instance["StatusPreparingTools"]);
        try
        {
            var paths = await _tools.UpdateAsync(new Progress<string>(OnToolOutput), _cts.Token);
            AppendLog(Loc.Instance.Format("LogToolsReady", paths.YtDlp));
            SetStatus(() => Loc.Instance["ToolsUpdated"]);
        }
        catch (OperationCanceledException)
        {
            SetStatus(() => Loc.Instance["StatusCancelled"]);
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            AppendLog(message);
            SetStatus(() => Loc.Instance.Format("StatusError", message));
        }
        finally
        {
            IsIndeterminate = false;
            IsBusy = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    private void OnStatus(DownloadStatus s)
    {
        switch (s.Phase)
        {
            case DownloadPhase.PreparingTools:
                IsIndeterminate = true;
                SetStatus(() => Loc.Instance["StatusPreparingTools"]);
                break;
            case DownloadPhase.FetchingInfo:
                IsIndeterminate = true;
                SetStatus(() => Loc.Instance["StatusFetchingInfo"]);
                break;
            case DownloadPhase.Downloading:
                IsIndeterminate = false;
                Progress = Math.Clamp(s.Percent, 0, 100);
                SetStatus(() => Loc.Instance.Format("StatusDownloading", s.Target, s.Percent));
                SpeedText = FormatSpeed(s);
                break;
            case DownloadPhase.Converting:
                IsIndeterminate = s.Percent < 100;
                Progress = 100;
                SpeedText = string.Empty;
                SetStatus(() => Loc.Instance.Format("StatusConverting", s.Target));
                break;
        }
    }

    private static string FormatSpeed(DownloadStatus s)
    {
        var parts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(s.Speed))
        {
            parts.Add($"{Loc.Instance["Speed"]}: {s.Speed}");
        }

        if (!string.IsNullOrWhiteSpace(s.Eta))
        {
            parts.Add($"{Loc.Instance["Eta"]}: {s.Eta}");
        }

        return string.Join("   ", parts);
    }

    private void OnToolOutput(string line)
    {
        if (line is "yt-dlp" or "ffmpeg" or "deno")
        {
            AppendLog(Loc.Instance.Format("LogToolDownloading", line));
            return;
        }

        // yt-dlp progress lines are noisy; keep everything else.
        if (!line.StartsWith("[download]", StringComparison.Ordinal) || line.Contains("Destination", StringComparison.Ordinal))
        {
            AppendLog(line.Trim());
        }
    }

    private void SetStatus(Func<string> render)
    {
        _statusRender = render;
        OnPropertyChanged(nameof(StatusText));
    }

    private void AppendLog(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _log.Append('[').Append(DateTime.Now.ToString("HH:mm:ss")).Append("] ").AppendLine(line);
        OnPropertyChanged(nameof(LogText));
    }

    private void ClearLog()
    {
        _log.Clear();
        OnPropertyChanged(nameof(LogText));
    }

    private void PasteFromClipboard()
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                Url = Clipboard.GetText().Trim();
            }
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Clipboard busy; ignore.
        }
    }

    private static void OpenFolder()
    {
        Directory.CreateDirectory(OutputDir);
        Process.Start(new ProcessStartInfo("explorer.exe", OutputDir) { UseShellExecute = true });
    }
}
