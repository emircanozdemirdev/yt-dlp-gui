# yt-dlp GUI (WPF)

Desktop GUI for `yt-dlp` built with WPF and MVVM.

## Features

- URL analyze flow with metadata and sorted format list
- Download queue with progress, speed, ETA, pause/resume, cancel, and retry
- Queue persistence across restarts for pending/paused jobs
- Cancel behavior that moves jobs to history and cleans temporary artifacts
- Download history persistence with multi-select actions (download again / delete)
- Batch actions in Queue and History (select all, clear selection, bulk operations)
- Settings for output directory, filename template, retries, proxy, and theme (`yt-dlp` / `ffmpeg` paths are managed automatically)

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

## Future Work

### High Priority

- Add integration tests for queue lifecycle (enqueue, pause/resume, cancel, history transfer) to prevent regressions
- Add explicit "Delete partial files on cancel" setting so cleanup behavior is user-configurable
- Add dynamic per-row button text (`Pause`/`Resume`) and status badges for clearer queue UX

### Medium Priority

- Add playlist mode toggle (`single video` vs `playlist`) in the analyze/download flow
- Improve progress reporting to avoid percent jumps between multi-stream phases
- Add output actions in history (`Open file`, `Open folder`) and optional search/filter

### Nice to Have

- Add update check UI for `yt-dlp` and `ffmpeg` with one-click refresh
- Add optional notifications for completed/failed downloads
- Add import/export for settings and history backups
