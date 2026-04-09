# yt-dlp GUI (WPF)

Desktop GUI for `yt-dlp` built with WPF and MVVM.

## Features

- URL analyze flow with metadata and format list
- Download queue with progress, speed, ETA, cancel
- Download history persistence
- Settings for output directory, filename template, retries, proxy (`yt-dlp` / `ffmpeg` paths are managed automatically)

## Requirements

- .NET 9 SDK
- Internet access on first launch (to download `yt-dlp` and `ffmpeg` if not bundled)
- Optional: place `yt-dlp.exe` and `ffmpeg.exe` under `YtDlpGui.App/tools/` before build to ship offline; otherwise they are cached under `%LocalAppData%\YtDlpGui\tools\`

## Run

```powershell
dotnet run --project YtDlpGui.App
```

## Test

```powershell
dotnet test
```

## Publish (Windows x64 single-file)

```powershell
dotnet publish YtDlpGui.App/YtDlpGui.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```
