# Riptide DLP

A cross-platform GUI wrapper for [yt-dlp](https://github.com/yt-dlp/yt-dlp). Drop URLs in, get videos out. Built with [Avalonia UI](https://avaloniaui.net/) + .NET 8.

![Platform: Windows / Linux / macOS](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
![.NET 8](https://img.shields.io/badge/.NET-8-purple)

---

<p align="center">
  <img src="docs/no-idea.gif" alt="The author, hard at work" width="400">
</p>

<p align="center"><em>The author, moments before shipping.</em></p>

---

## Prerequisites

Riptide DLP shells out to external tools — it's a GUI, not a downloader itself. You need:

| Tool | Required? | What it does |
|---|---|---|
| [yt-dlp](https://github.com/yt-dlp/yt-dlp/releases) | **Required** | The actual downloader. Without this the app is just a really pretty list of links. |
| [FFmpeg](https://ffmpeg.org/download.html) | **Required** | Merges video + audio streams (YouTube ships them separately). Also handles thumbnails, subtitles, and conversions. |
| [Node.js](https://nodejs.org/) | Optional | Solves YouTube's JavaScript challenge for trickier URLs. Most videos work without it, but some won't. |

> **Heads up:** Riptide DLP checks for these automatically on first launch and shows a dialog if anything is missing. You can re-open that dialog any time from **Help → Prerequisites**.

### Quick install

**Windows (winget)**
```powershell
winget install yt-dlp.yt-dlp
winget install Gyan.FFmpeg
winget install OpenJS.NodeJS
```

**macOS (Homebrew)**
```bash
brew install yt-dlp ffmpeg node
```

**Linux (apt / pip)**
```bash
sudo apt install ffmpeg nodejs        # FFmpeg + Node from apt
pip install yt-dlp                    # yt-dlp from pip (gets you the latest)
```

Make sure all three are on your `$PATH` — the app looks for `yt-dlp`, `ffmpeg`, and `node` by name.

---

## Installation

Pre-built self-contained binaries (no .NET runtime required) are on the [**Releases page**](https://github.com/quietlydismantled/riptide-dlp/releases).

| Platform | File |
|---|---|
| Windows x64 | `riptide-dlp-vX.Y.Z-windows.zip` |
| Linux x64 | `riptide-dlp-vX.Y.Z-linux.tar.gz` |
| macOS x64 (Intel) | `riptide-dlp-vX.Y.Z-macos-x64.tar.gz` |
| macOS ARM64 (Apple Silicon) | `riptide-dlp-vX.Y.Z-macos-arm64.tar.gz` |

**Windows:** Extract the zip, double-click `RiptideDlp.exe`.

**Linux:**
```bash
tar -xzf riptide-dlp-*.tar.gz
chmod +x RiptideDlp
./RiptideDlp
```

**macOS:**
```bash
tar -xzf riptide-dlp-*.tar.gz
chmod +x RiptideDlp
# First run only — remove the quarantine flag if macOS blocks it:
xattr -d com.apple.quarantine ./RiptideDlp
./RiptideDlp
```

---

## Quick Start

1. Drop `.url`, `.txt`, or `.lst` files onto the **drop zone** (one URL per line in text files)
2. Or **click the drop zone** to browse for files
3. Or use **`+ Files`**, **`+ URL`**, or **Paste** in the toolbar to add URLs directly
4. Downloads start automatically — up to N at a time, configurable in the toolbar or **Options**

---

## Features

### Adding URLs

Riptide DLP accepts URLs through several paths — use whichever fits your workflow:

| Method | How |
|---|---|
| **Drag & drop files** | Drop `.url`, `.txt`, or `.lst` files onto the drop zone. Each line in a text file becomes one download. |
| **Drag & drop URLs** | Drag a URL string (e.g. from a browser address bar) directly onto the drop zone. |
| **`+ Files` / `Ctrl+O`** | Opens a file picker (multi-select). Accepts `.url`, `.txt`, `.lst`, and all files. |
| **`+ URL` / `Ctrl+U`** | Opens a dialog where you can type or paste one or more URLs. |
| **Paste / `Ctrl+V`** | Reads your clipboard and imports any URLs it finds. |

All sources split on newlines automatically. Blank lines and exact duplicates are silently ignored.

---

### Download Queue

The main table shows all downloads with live-updating columns:

| Column | What it shows |
|---|---|
| **#** | Sequential ID |
| **Title / URL** | Video title (fetched from metadata), or the URL if the title isn't available yet |
| **Status** | Current state of the download |
| **Progress** | ASCII progress bar + percentage |
| **Speed** | Current transfer speed |
| **ETA** | Estimated time remaining |
| **Size** | File size |

Row background colors reflect download state at a glance:

- Neutral — queued
- Blue — downloading
- Green — complete
- Red — error
- Grey — cancelled

Column widths are resizable and saved between sessions.

---

### Row Actions

| Action | How to do it |
|---|---|
| **Open downloaded file** | Double-click a row |
| **Open output folder** | Right-click → *Open output folder* |
| **Cancel** | Right-click → *Cancel* |
| **Retry** a failed/cancelled item | Right-click → *Retry* |
| **Remove** from list | Right-click → *Remove*, or press `Delete` |
| **Copy URL** to clipboard | Right-click → *Copy URL* |

You can multi-select rows with `Shift+Click` or `Ctrl+Click`. `Delete` removes all selected rows at once.

---

### Console Panel

The Console panel shows the raw yt-dlp output — useful for debugging errors or watching what's happening under the hood.

- Toggle it with the **Console** button in the toolbar
- Drag the **splitter** between the queue and console to resize it
- **Select a row** in the download queue to filter the console to that download's output only
- Deselect (click empty space) to see all output

---

### Toolbar

| Button | Action |
|---|---|
| **+ Files** | Add files (file picker) |
| **+ URL** | Add URLs via dialog |
| **Paste** | Import URLs from clipboard |
| **Clear Done** | Remove all completed and errored items from the list |
| **Cancel All** | Cancel all running and queued downloads |
| **Console** | Toggle the console panel |
| **Concurrent ↑↓** | Set how many downloads run in parallel (1–16) |

---

### Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+O` | Add files (file picker) |
| `Ctrl+U` | Add URL dialog |
| `Ctrl+V` | Paste URLs from clipboard |
| `Delete` | Remove selected row(s) from the list |

---

### Menus

**File**
- Add Files… (`Ctrl+O`)
- Add URL… (`Ctrl+U`)
- Exit

**Options**
- Settings… — opens the full Options dialog
- Dark mode — toggle theme
- Open output folder — opens the configured download directory

**Tools**
- Update yt-dlp — downloads the latest yt-dlp binary
- Kill all yt-dlp processes — emergency stop for all running downloads

**Help**
- Prerequisites… — shows the tool check dialog
- About

---

### Options Dialog

Open from **Options → Settings…**.

| Setting | Default | Notes |
|---|---|---|
| **Concurrent downloads** | 3 | Also adjustable in the toolbar. Range: 1–20. |
| **Output directory** | `~/Videos` | Where downloaded files are saved. Browse button included. |
| **Format string** | `bestvideo+bestaudio/best` | yt-dlp format selector. Drop-down includes common presets; fully editable. |
| **Rate limit** | *(none)* | Throttle download speed. e.g. `2M` for 2 MB/s, `500K` for 500 KB/s. |
| **Cookies from browser** | *(none)* | Pull cookies from a running browser (Chrome, Firefox, Edge, etc.) for age-restricted or logged-in content. |
| **Cookie file (.txt)** | *(none)* | Path to a Netscape-format cookie export file. Alternative to browser cookies. |
| **JS runtime** | `node` | JavaScript runtime for YouTube's n-challenge. `node` is recommended; `deno` also works. |
| **YouTube player client** | *(none)* | Override the player client string passed to YouTube. Leave blank unless you know what you're doing. |
| **Subtitle languages** | `en` | Comma-separated BCP-47 language tags. e.g. `en,es,fr`. Used when *Write subtitles* is on. |
| **Extra yt-dlp flags** | *(none)* | Any additional flags appended verbatim to every yt-dlp call. |
| **Audio only** | off | Extracts audio and saves as MP3. Requires FFmpeg. |
| **Skip existing files** | on | Passes `--no-overwrites`; won't re-download files that already exist. |
| **Ignore errors** | on | Continues a playlist even if individual items fail. |
| **Embed thumbnail** | off | Embeds the video thumbnail as album art. Requires FFmpeg. |
| **Write subtitles** | off | Downloads and saves subtitles in the configured languages. |
| **SponsorBlock** | off | Automatically removes sponsor segments using the [SponsorBlock](https://sponsor.ajay.app/) database. |
| **Dark mode** | on | Toggleable here or from the Options menu. |

Settings are saved to:
- **Windows:** `%APPDATA%\riptide-dlp\settings.json`
- **Linux / macOS:** `~/.config/riptide-dlp/settings.json`

---

### Prerequisites Dialog

Available from **Help → Prerequisites…** (also opens automatically on launch if anything required is missing).

Shows the install status and detected version of yt-dlp, FFmpeg, and Node.js. Each entry has a **Get it →** button that opens the official download page. Hit **Re-check** after installing a tool to update the status without restarting the app.

---

## Build from Source

Requires [**.NET 8 SDK**](https://dotnet.microsoft.com/download/dotnet/8.0). No other global tools needed.

```bash
git clone https://github.com/quietlydismantled/riptide-dlp.git
cd riptide-dlp
dotnet build RiptideDlp.sln
dotnet run --project src/RiptideDlp
```

**Self-contained single-file publish** (replace `win-x64` with your target RID):
```bash
dotnet publish src/RiptideDlp -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o out/win-x64
```

Available RIDs: `win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`.

**Tag-based releases** are handled automatically by GitHub Actions — push a `v*` tag and it builds all four platforms and creates a Release with attached binaries.

---

## FAQ / Troubleshooting

**"yt-dlp not found" on startup**  
Install yt-dlp and make sure it's on your `PATH`. On Windows you can also drop the `yt-dlp.exe` in the same folder as `RiptideDlp.exe`.

**Video downloads fine but there's no audio (or vice versa)**  
FFmpeg is required to merge the separate video and audio streams that YouTube delivers. Install it and re-check via Help → Prerequisites.

**YouTube n-challenge / `nsig` errors**  
Install Node.js. It's listed as optional but YouTube increasingly requires it for higher-quality streams.

**Age-restricted or members-only content fails**  
Set **Cookies from browser** in Options to your logged-in browser, or export a cookie file and point the **Cookie file** setting at it.

**Download is stuck at 0% with no visible progress**  
Open the Console panel and select the stuck row — the raw yt-dlp output will show what's going wrong.

**"Clear Done" doesn't remove a cancelled item**  
Clear Done removes completed and errored items. Cancelled items are included too. If a partial file (`.part` or `.ytdl`) was left behind, it's deleted along with the entry.

**Linux: app won't launch**  
Make sure the binary has execute permission: `chmod +x RiptideDlp`

**macOS: "cannot be opened because the developer cannot be verified"**  
Either right-click → Open the first time, or strip the quarantine attribute:
```bash
xattr -d com.apple.quarantine ./RiptideDlp
```
