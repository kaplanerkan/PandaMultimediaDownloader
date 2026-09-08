# 🐼 Panda Multimedia Downloader

A lightweight Windows desktop app (.NET 9 / WPF) that downloads videos from
YouTube, Instagram, Facebook and hundreds of other sites as **MP4**, **MP3**, or both.
Turquoise-themed UI, available in **Türkçe / Deutsch / English**.

![Panda Multimedia Downloader](docs/screenshot.png)

## Features

- **One URL, one click.** Paste a link, pick MP4 and/or MP3, press Download.
- **MP4 + MP3 together.** When both are selected the MP3 is extracted from the
  downloaded MP4 with ffmpeg, so the media is fetched only once.
- **Sensible file names.** Files are named after the media title. If no title is
  available the name is derived from the URL, e.g. `youtube_dQw4w9WgXcQ` or
  `instagram_DavUIURsVSl`. Duplicates get `_2`, `_3`, ...
- **Output folder next to the app:** `downloaded/`.
- **Progress, speed and ETA**, a log panel and a cancel button.
- **Three languages**, switchable at runtime and remembered in `settings.json`.
- **Browser cookies option** for sites that require a login (private Instagram
  posts, some Facebook videos): Chrome, Edge, Firefox, Brave, Opera, Vivaldi.
- **Zero setup.** `yt-dlp`, `ffmpeg` and `deno` are downloaded automatically into
  `tools/` on first run. If `ffmpeg` is already on your `PATH`, it is reused.
- **Update tools** button re-downloads `yt-dlp` (sites change often; a stale
  `yt-dlp` is the most common cause of download errors).

## Supported sites

Everything supported by [yt-dlp](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md):
YouTube (videos, Shorts, Music), Instagram (posts, reels), Facebook (videos, reels,
fb.watch), TikTok, Twitter/X, Vimeo, Dailymotion, Twitch clips and many more.

## Getting started

Requirements: Windows 10/11, [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```powershell
git clone https://github.com/kaplanerkan/PandaMultimediaDownloader.git
cd PandaMultimediaDownloader
dotnet run --project src/PandaMultimediaDownloader
```

Publish a single-file build:

```powershell
dotnet publish src/PandaMultimediaDownloader -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

The executable lands in `src/PandaMultimediaDownloader/bin/Release/net9.0-windows/win-x64/publish/`.
Copy the whole folder anywhere; `tools/`, `downloaded/` and `settings.json` are
created next to it.

## Tests

```powershell
dotnet test
```

xUnit tests cover file-name derivation, sanitising, unique naming and localization.

## Project layout

```
src/PandaMultimediaDownloader/
  Localization/Loc.cs           TR / DE / EN strings, runtime language switch
  Services/ToolManager.cs       locates or downloads yt-dlp, ffmpeg, deno
  Services/DownloadService.cs   download pipeline, MP3 extraction, progress
  Services/FileNameHelper.cs    title / URL -> safe, unique file name
  Services/AppSettings.cs       settings.json persistence
  ViewModels/MainViewModel.cs   UI state and commands
  Themes/Turquoise.xaml         palette and control styles
tests/PandaMultimediaDownloader.Tests/
```

## Troubleshooting

| Symptom | What to do |
|---|---|
| "Sign in to confirm you're not a bot" / login required | Select your browser under **Use browser cookies** and retry. |
| Download fails right after a site update | Press **Update tools**. |
| First start takes a while | `yt-dlp` (~17 MB), `deno` (~90 MB) and, if missing, `ffmpeg` are being downloaded once. |

## Türkçe

YouTube, Instagram, Facebook ve yt-dlp'nin desteklediği diğer sitelerden medyayı
MP4, MP3 veya her ikisi olarak indiren .NET 9 / WPF uygulaması.

- URL yapıştır, format seç, **İndir**.
- Dosya adı medya başlığından gelir; başlık yoksa URL'den türetilir.
- Çıktı uygulamanın yanındaki `downloaded/` klasörüne yazılır.
- Dil: Türkçe / Deutsch / English, çalışırken değiştirilebilir.
- Oturum isteyen içerikler için **Tarayıcı çerezlerini kullan** seçeneği.
- `yt-dlp`, `ffmpeg`, `deno` ilk açılışta otomatik iner; hata alırsan önce
  **Araçları güncelle** dene.

## License

MIT
