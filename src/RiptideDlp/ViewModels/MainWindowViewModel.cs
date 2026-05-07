using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RiptideDlp.Core.Models;
using RiptideDlp.Core.Services;
using RiptideDlp.Models;

namespace RiptideDlp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    readonly DownloadService _svc = new();
    AppConfig _cfg = AppConfig.Load();
    int _nextId = 1;
    bool _chromiumWarnShown;
    bool _nodeWarnLogged;
    int _tipTick;
    int _tipIndex;
    Process?      _updateProcess;
    StreamReader? _updateOut;
    StreamReader? _updateErr;
    bool          _updateRunning;
    readonly DispatcherTimer _timer;

    public ObservableCollection<DownloadItemViewModel> Downloads { get; } = [];
    public ObservableCollection<LogLine>               LogLines  { get; } = [];

    [ObservableProperty] bool   _isConsoleVisible;
    [ObservableProperty] string _statusText    = "  Ready";
    [ObservableProperty] string _windowTitle   = "Riptide DLP";
    [ObservableProperty] string _currentTip    = Tips[0] + "  ";
    [ObservableProperty] bool   _isDarkMode;
    [ObservableProperty] bool   _updateButtonEnabled = true;
    [ObservableProperty] string _updateButtonText    = "Update yt-dlp";

    // Set by View code-behind — platform calls that need TopLevel
    public Func<Task<IReadOnlyList<string>>>?    RequestOpenFiles  { get; set; }
    public Func<Task<string?>>?                  RequestAddUrl     { get; set; }
    public Func<Task<string?>>?                  RequestPaste      { get; set; }
    public Func<string, string, Task<bool>>?     RequestConfirm    { get; set; }
    public Func<AppConfig, Task<AppConfig?>>?    RequestOptions    { get; set; }
    public Action<string>?                       RequestCopyText   { get; set; }

    static readonly string[] Tips =
    [
        "Tip: Drag .url, .txt or .lst files straight onto the drop zone. Ctrl+O works too.",
        "Tip: Right-click any row to cancel, retry, copy URL and more.",
        "Tip: Firefox cookies work with the browser open. Chrome/Edge? Close them. Close them hard.",
        "Tip: Ctrl+A selects all rows. Delete removes them. You didn't need those anyway.",
        "Tip: Node.js installed? Set JS runtime to 'node' in Options. YouTube will cooperate more.",
        "Tip: Console button shows raw yt-dlp output. Great for debugging. Or existential dread.",
        "Tip: SponsorBlock silently nukes the 'smash that subscribe' monologues. You're welcome.",
        "Tip: Concurrent downloads set low = polite to servers. Servers remember kindness. Probably.",
        "Tip: Paste a URL straight into the drop zone - it's not just for files.",
        "Tip: Double-click any row to open the output folder. The files are in there. Hopefully.",
        "Tip: bestvideo+bestaudio/best is carrying the whole format string on its back.",
        "Tip: Ctrl+V in the download list pastes URLs from clipboard. Efficient.",
        "Tip: Cookie file skips the 'please close your browser' dance entirely.",
        "Tip: Rate limit (e.g. '2M') keeps background downloads from eating all the bandwidth.",
        "Tip: Update yt-dlp often. YouTube patches itself constantly. It's a whole lifestyle.",
        "Tip: Embed thumbnail bakes the video art into the file. Very professional.",
        "Tip: Column widths are saved when you close. Resize to taste.",
        "Tip: Drop a .txt with one URL per line and queue the whole batch at once.",
        "Tip: Dark mode is in Options > Dark mode. Your retinas already knew that.",
        "Tip: Kill yt-dlp is the nuclear option. For when it knows what it did.",
    ];

    public MainWindowViewModel()
    {
        _isDarkMode = _cfg.DarkMode;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    // ── Commands ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    async Task AddFileAsync()
    {
        if (RequestOpenFiles == null) return;
        var files = await RequestOpenFiles();
        var urls  = files.SelectMany(DownloadService.GetUrlsFromPath).ToList();
        AddUrlsToQueue(urls);
    }

    [RelayCommand]
    async Task AddUrlAsync()
    {
        if (RequestAddUrl == null) return;
        var raw = await RequestAddUrl();
        if (raw == null) return;
        var urls = raw.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                      .Select(u => u.Trim())
                      .Where(u => u.Contains("://"))
                      .ToList();
        if (urls.Count > 0) AddUrlsToQueue(urls);
    }

    [RelayCommand]
    async Task PasteAsync()
    {
        if (RequestPaste == null) return;
        var text = await RequestPaste();
        if (string.IsNullOrWhiteSpace(text)) { StatusText = "  Clipboard contains no URLs"; return; }
        var urls = text.Split(['\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries)
                       .Where(u => u.StartsWith("http://",  StringComparison.OrdinalIgnoreCase) ||
                                   u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                       .ToList();
        if (urls.Count > 0) AddUrlsToQueue(urls);
        else StatusText = "  Clipboard contains no URLs";
    }

    [RelayCommand]
    void ClearDone()
    {
        var toRemove = Downloads.Where(vm => vm.Status is DlStatus.Complete or DlStatus.Error or DlStatus.Cancelled).ToList();
        foreach (var vm in toRemove) RemoveItem(vm);
        UpdateStatus();
    }

    [RelayCommand]
    void CancelAll()
    {
        foreach (var vm in Downloads.Where(d => d.Status == DlStatus.Downloading).ToList())
            _svc.CancelDlItem(vm.Model, GetModels(), _cfg, Log);
        RefreshAll(); UpdateStatus();
    }

    [RelayCommand]
    void ToggleConsole() => IsConsoleVisible = !IsConsoleVisible;

    [RelayCommand]
    async Task KillAllAsync()
    {
        if (RequestConfirm == null) return;
        var ok = await RequestConfirm(
            "Kill ALL yt-dlp.exe processes on this machine?\n\nThis includes any not started by this app.",
            "Kill yt-dlp");
        if (!ok) return;
        foreach (var vm in Downloads.Where(d => d.Status == DlStatus.Downloading).ToList())
        {
            try { vm.Model.Process?.Kill(); } catch { }
            _svc.ClearDlProcess(vm.Model);
            _svc.DecrementActive();
            vm.Model.Status = DlStatus.Cancelled;
        }
        foreach (var p in Process.GetProcessesByName("yt-dlp"))
            try { p.Kill(); } catch { }
        RefreshAll(); UpdateStatus();
    }

    [RelayCommand]
    void UpdateYtDlp()
    {
        if (_updateRunning) return;
        _updateRunning = true;
        UpdateButtonEnabled = false; UpdateButtonText = "Updating...";
        IsConsoleVisible = true;
        Log("[UPDATE] Running yt-dlp --update ...");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp", UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
            };
            psi.ArgumentList.Add("--update");
            var p = new Process { StartInfo = psi };
            p.Start();
            _updateProcess = p;
            _updateOut     = p.StandardOutput;
            _updateErr     = p.StandardError;
        }
        catch (Exception ex)
        {
            Log($"[ERR UPDATE] Could not launch yt-dlp: {ex.Message}");
            _updateRunning = false;
            UpdateButtonEnabled = true; UpdateButtonText = "Update yt-dlp";
        }
    }

    [RelayCommand]
    async Task ShowOptionsAsync()
    {
        if (RequestOptions == null) return;
        var newCfg = await RequestOptions(_cfg);
        if (newCfg == null) return;
        _cfg = newCfg;
        _cfg.Save();
        _chromiumWarnShown = false;
        _nodeWarnLogged    = false;
        IsDarkMode = _cfg.DarkMode;
        ApplyTheme();
        _svc.StartNextInQueue(GetModels(), _cfg, Log);
    }

    [RelayCommand]
    async Task OpenOutputDirAsync()
    {
        var p = _cfg.OutputPath;
        if (string.IsNullOrEmpty(p) || !Directory.Exists(p))
        {
            if (RequestConfirm != null)
                await RequestConfirm($"Output folder not found:\n{p}", "yt-dlp");
            return;
        }
        OpenFolder(p);
    }

    [RelayCommand]
    void ToggleDarkMode()
    {
        _cfg.DarkMode = !_cfg.DarkMode;
        _cfg.Save();
        IsDarkMode = _cfg.DarkMode;
        ApplyTheme();
    }

    [RelayCommand]
    void CancelItem(DownloadItemViewModel vm)
    {
        if (vm.Status is DlStatus.Queued or DlStatus.Downloading)
            _svc.CancelDlItem(vm.Model, GetModels(), _cfg, Log);
        vm.Refresh(); UpdateStatus();
    }

    [RelayCommand]
    void RetryItem(DownloadItemViewModel vm)
    {
        _svc.RetryDlItem(vm.Model, GetModels(), _cfg, Log);
        vm.Refresh(); UpdateStatus();
    }

    [RelayCommand]
    void RemoveItem(DownloadItemViewModel vm)
    {
        if (vm.Status == DlStatus.Downloading)
            _svc.CancelDlItem(vm.Model, GetModels(), _cfg, Log);
        Downloads.Remove(vm);
        UpdateStatus();
    }

    [RelayCommand]
    void CopyUrl(DownloadItemViewModel vm) => RequestCopyText?.Invoke(vm.Url);

    [RelayCommand]
    void OpenItemFolder(DownloadItemViewModel vm) => OpenFolder(_cfg.OutputPath);

    // ── Public surface for View ──────────────────────────────────────────────────

    public void AddUrlsFromDrop(IEnumerable<string> paths)
        => AddUrlsToQueue(paths.SelectMany(DownloadService.GetUrlsFromPath).ToList());

    public void AddUrlsFromText(string text)
    {
        var urls = text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                       .Where(u => u.StartsWith("http://",  StringComparison.OrdinalIgnoreCase) ||
                                   u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                       .ToList();
        if (urls.Count > 0) AddUrlsToQueue(urls);
    }

    public void SaveColumnWidths(int[] widths) { _cfg.ColumnWidths = widths; _cfg.Save(); }
    public int[] GetColumnWidths() => _cfg.ColumnWidths;

    public void OnClosing()
    {
        _timer.Stop();
        foreach (var vm in Downloads.Where(d => d.Model.Process != null && !d.Model.Process.HasExited))
        {
            try { vm.Model.Process!.Kill(); } catch { }
            _svc.ClearDlProcess(vm.Model);
        }
    }

    // ── Internals ────────────────────────────────────────────────────────────────

    void AddUrlsToQueue(IEnumerable<string> urls)
    {
        if (!_chromiumWarnShown && !string.IsNullOrWhiteSpace(_cfg.CookiesFrom) &&
            DownloadService.IsChromiumBrowser(_cfg.CookiesFrom) &&
            DownloadService.IsBrowserRunning(_cfg.CookiesFrom))
        {
            _chromiumWarnShown = true;
            var browser = _cfg.CookiesFrom.Split(':')[0];
            RequestConfirm?.Invoke(
                $"{browser} appears to be running.\n\nChromium-based browsers must be fully closed before yt-dlp can read their cookies. Close {browser}, then retry.",
                "Close browser first");
        }
        if (!_nodeWarnLogged && _cfg.JsRuntime == "node" && !DownloadService.IsRuntimeAvailable("node"))
        {
            _nodeWarnLogged = true;
            Log("[WARN] JsRuntime is 'node' but node.exe not found on PATH. n-challenge solving will fail.");
        }
        foreach (var u in urls.Where(u =>
            u.StartsWith("http://",  StringComparison.OrdinalIgnoreCase) ||
            u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            if (Downloads.Any(d => d.Url == u)) continue;
            var model = new DownloadItem(_nextId++, u);
            Downloads.Add(new DownloadItemViewModel(model));
        }
        _svc.StartNextInQueue(GetModels(), _cfg, Log);
        UpdateStatus();
    }

    void RefreshAll() { foreach (var vm in Downloads) vm.Refresh(); }
    IReadOnlyList<DownloadItem> GetModels() => Downloads.Select(vm => vm.Model).ToList();

    void Log(string line)
    {
        bool isErr = line.StartsWith("[ERR ", StringComparison.Ordinal);
        LogLines.Add(new LogLine(line, isErr));
        if (LogLines.Count > 5000) LogLines.RemoveAt(0);
    }

    void UpdateStatus()
    {
        var q     = Downloads.Count(d => d.Status == DlStatus.Queued);
        var a     = Downloads.Count(d => d.Status == DlStatus.Downloading);
        var done  = Downloads.Count(d => d.Status == DlStatus.Complete);
        var e     = Downloads.Count(d => d.Status == DlStatus.Error);
        var total = Downloads.Count;
        StatusText  = $"  Active: {a}   Queued: {q}   Done: {done}   Errors: {e}";
        WindowTitle = a > 0     ? $"yt-dlp Download Manager  ({a}/{total} active)"
                    : total > 0 ? $"yt-dlp Download Manager  ({done} done{(e > 0 ? $", {e} errors" : "")})"
                    :              "yt-dlp Download Manager";
    }

    void ApplyTheme()
    {
        if (Avalonia.Application.Current != null)
            Avalonia.Application.Current.RequestedThemeVariant =
                _cfg.DarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    static void OpenFolder(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())    Process.Start("explorer.exe", path);
            else if (OperatingSystem.IsMacOS()) Process.Start("open", path);
            else Process.Start(new ProcessStartInfo("xdg-open", path) { UseShellExecute = true });
        }
        catch { }
    }

    void OnTimerTick(object? sender, EventArgs e)
    {
        _svc.PollDownloads(GetModels(), _cfg, Log, () => _svc.StartNextInQueue(GetModels(), _cfg, Log));
        RefreshAll();
        UpdateStatus();
        if (++_tipTick % 24 == 0)
        {
            _tipIndex  = (_tipIndex + 1) % Tips.Length;
            CurrentTip = Tips[_tipIndex] + "  ";
        }
        if (_updateProcess != null)
        {
            while (_updateOut?.Peek() >= 0) Log($"[UPDATE] {_updateOut.ReadLine()}");
            while (_updateErr?.Peek() >= 0) Log($"[ERR UPDATE] {_updateErr.ReadLine()}");
            if (_updateProcess.HasExited)
            {
                while (_updateOut?.Peek() >= 0) Log($"[UPDATE] {_updateOut.ReadLine()}");
                while (_updateErr?.Peek() >= 0) Log($"[ERR UPDATE] {_updateErr.ReadLine()}");
                Log($"[UPDATE] Finished (exit code {_updateProcess.ExitCode}).");
                try { _updateProcess.Dispose(); } catch { }
                _updateProcess = null; _updateOut = null; _updateErr = null;
                _updateRunning = false;
                UpdateButtonEnabled = true; UpdateButtonText = "Update yt-dlp";
            }
        }
    }
}
