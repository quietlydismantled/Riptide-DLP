using System.Diagnostics;
using System.Text.RegularExpressions;
using RiptideDlp.Core.Models;

namespace RiptideDlp.Core.Services;

public class DownloadService
{
    static readonly Regex PctRx   = new(@"(?i)^\[download\]\s+([\d.]+)%",          RegexOptions.Compiled);
    static readonly Regex SizeRx  = new(@"(?i)\bof\s+~?\s*(.+?)\s+(?:at|in)\b",   RegexOptions.Compiled);
    static readonly Regex SpeedRx = new(@"(?i)\bat\s+(.+?/s)\b",                   RegexOptions.Compiled);
    static readonly Regex EtaRx   = new(@"(?i)\bETA\s+(\S+)",                      RegexOptions.Compiled);
    static readonly Regex DstRx     = new(@"(?i)\[download\]\s+Destination:\s+(.+)",              RegexOptions.Compiled);
    static readonly Regex AlreadyRx = new(@"(?i)\[download\]\s+(.+?)\s+has already been downloaded", RegexOptions.Compiled);

    int _activeCount;
    public int ActiveCount => _activeCount;

    // ── URL parsing ─────────────────────────────────────────────────────────────

    public static IEnumerable<string> GetUrlsFromPath(string p)
    {
        if (!File.Exists(p))
        {
            if (Regex.IsMatch(p, @"^https?://\S+$")) return [p];
            return [];
        }
        try
        {
            var ext = Path.GetExtension(p).ToLowerInvariant();
            if (ext == ".url")
            {
                var m = Regex.Match(File.ReadAllText(p), @"(?im)^\s*URL\s*=\s*(.+?)\s*$");
                return m.Success ? [m.Groups[1].Value.Trim()] : [];
            }
            return File.ReadAllLines(p).Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l));
        }
        catch { return []; }
    }

    // ── Argument builder ────────────────────────────────────────────────────────

    public static List<string> BuildArgs(AppConfig cfg, string url)
    {
        var a = new List<string> { "--newline", "--no-color", "--progress" };
        if (cfg.IgnoreErrors)   a.Add("--ignore-errors");
        if (cfg.NoOverwrites)   a.Add("--no-overwrites");
        if (cfg.EmbedThumbnail) a.Add("--embed-thumbnail");
        if (cfg.SponsorBlock)   { a.Add("--sponsorblock-remove"); a.Add("sponsor"); }
        if (cfg.WriteSubtitles) { a.Add("--write-sub"); a.Add("--sub-lang"); a.Add(cfg.SubLang); }
        if (!string.IsNullOrWhiteSpace(cfg.OutputPath))  { a.Add("--paths"); a.Add(cfg.OutputPath); }
        if (cfg.AudioOnly) { a.Add("-x"); a.Add("--audio-format"); a.Add("mp3"); }
        else               { a.Add("-f"); a.Add(cfg.Format); }
        if (!string.IsNullOrWhiteSpace(cfg.RateLimit))   { a.Add("-r"); a.Add(cfg.RateLimit); }
        if (!string.IsNullOrWhiteSpace(cfg.CookiesFrom)) { a.Add("--cookies-from-browser"); a.Add(cfg.CookiesFrom); }
        if (!string.IsNullOrWhiteSpace(cfg.CookieFile))  { a.Add("--cookies"); a.Add(cfg.CookieFile); }
        if (!string.IsNullOrWhiteSpace(cfg.JsRuntime))   { a.Add("--js-runtimes"); a.Add(cfg.JsRuntime); }
        if (!string.IsNullOrWhiteSpace(cfg.PlayerClient))
        {
            bool hasCookies = !string.IsNullOrWhiteSpace(cfg.CookiesFrom) || !string.IsNullOrWhiteSpace(cfg.CookieFile);
            if (!hasCookies) { a.Add("--extractor-args"); a.Add($"youtube:player_client={cfg.PlayerClient}"); }
        }
        if (!string.IsNullOrWhiteSpace(cfg.ExtraArgs))
            a.AddRange(cfg.ExtraArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        a.Add("--"); a.Add(url);
        return a;
    }

    // ── Output parsing ──────────────────────────────────────────────────────────

    public bool UpdateDlFromLine(DownloadItem dl, string line, bool isError, Action<string> logLine)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        var t = line.Trim();
        bool isDlLine = t.StartsWith("[download]", StringComparison.OrdinalIgnoreCase);
        logLine(isError && !isDlLine ? $"[ERR {dl.Id}] {line}" : $"[{dl.Id}] {line}");

        var m = PctRx.Match(t);
        if (m.Success)
        {
            dl.Pct = Math.Min(100.0, double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
            var sz = SizeRx.Match(t);  if (sz.Success) dl.Size  = sz.Groups[1].Value.Trim();
            var sp = SpeedRx.Match(t); if (sp.Success) dl.Speed = sp.Groups[1].Value.Trim();
            var et = EtaRx.Match(t);   if (et.Success) dl.ETA   = et.Groups[1].Value.Trim();
            else if (dl.Pct >= 100)                     dl.ETA   = "Done";
        }
        else
        {
            dl.LastLine = isError && !isDlLine ? $"ERR: {t}" : t;
        }

        var d = DstRx.Match(t);
        if (d.Success)
            dl.Title = Path.GetFileNameWithoutExtension(d.Groups[1].Value.Trim());
        else
        {
            var a = AlreadyRx.Match(t);
            if (a.Success) dl.Title = Path.GetFileNameWithoutExtension(a.Groups[1].Value.Trim());
        }

        return true;
    }

    public void DrainOutputQueue(DownloadItem dl, Action<string> logLine)
    {
        while (dl.OutputQueue.TryDequeue(out var rec))
            UpdateDlFromLine(dl, rec.Line, rec.IsError, logLine);
    }

    // ── Process lifecycle ───────────────────────────────────────────────────────

    public void StartDlItem(DownloadItem dl, AppConfig cfg, Action<string> logLine)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "yt-dlp",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
        };
        foreach (var arg in BuildArgs(cfg, dl.Url)) psi.ArgumentList.Add(arg);
        try
        {
            dl.OutputQueue = new();
            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

            DataReceivedEventHandler outH = (_, e) => { if (e.Data != null) dl.OutputQueue.Enqueue(new(e.Data, false)); };
            DataReceivedEventHandler errH = (_, e) => { if (e.Data != null) dl.OutputQueue.Enqueue(new(e.Data, true)); };
            p.OutputDataReceived += outH;
            p.ErrorDataReceived  += errH;

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            dl.Process    = p;
            dl.OutHandler = outH;
            dl.ErrHandler = errH;
            dl.Status     = DlStatus.Downloading;
            _activeCount++;
        }
        catch (Exception ex)
        {
            dl.Status = DlStatus.Error;
            dl.ETA    = "Launch failed";
            logLine($"[ERR {dl.Id}] {ex.Message}");
        }
    }

    public void StartNextInQueue(IReadOnlyList<DownloadItem> downloads, AppConfig cfg, Action<string> logLine)
    {
        while (_activeCount < cfg.Concurrent)
        {
            var next = downloads.FirstOrDefault(d => d.Status == DlStatus.Queued);
            if (next == null) break;
            StartDlItem(next, cfg, logLine);
        }
    }

    public void ClearDlProcess(DownloadItem dl)
    {
        if (dl.Process != null)
        {
            if (dl.OutHandler != null) { dl.Process.OutputDataReceived -= dl.OutHandler; dl.OutHandler = null; }
            if (dl.ErrHandler != null) { dl.Process.ErrorDataReceived  -= dl.ErrHandler; dl.ErrHandler = null; }
            try { dl.Process.CancelOutputRead(); } catch { }
            try { dl.Process.CancelErrorRead();  } catch { }
            try { dl.Process.Dispose(); }          catch { }
            dl.Process = null;
        }
    }

    public void CancelDlItem(DownloadItem dl, IReadOnlyList<DownloadItem> downloads, AppConfig cfg, Action<string> logLine)
    {
        if (dl.Process != null && !dl.Process.HasExited)
        {
            try { dl.Process.Kill(); } catch { }
            _activeCount = Math.Max(0, _activeCount - 1);
        }
        ClearDlProcess(dl);
        dl.Status = DlStatus.Cancelled;
        StartNextInQueue(downloads, cfg, logLine);
    }

    public void RetryDlItem(DownloadItem dl, IReadOnlyList<DownloadItem> downloads, AppConfig cfg, Action<string> logLine)
    {
        if (dl.Process != null && !dl.Process.HasExited)
        {
            try { dl.Process.Kill(); } catch { }
            _activeCount = Math.Max(0, _activeCount - 1);
        }
        ClearDlProcess(dl);
        dl.Status = DlStatus.Queued; dl.Pct = 0;
        dl.Speed = "—"; dl.ETA = "—"; dl.Size = "—";
        dl.Title = dl.Url; dl.LastLine = "";
        dl.OutputQueue = new();
        StartNextInQueue(downloads, cfg, logLine);
    }

    public void PollDownloads(IReadOnlyList<DownloadItem> downloads, AppConfig cfg, Action<string> logLine, Action startNext)
    {
        foreach (var dl in downloads.ToList())
        {
            if (dl.Status != DlStatus.Downloading) continue;
            DrainOutputQueue(dl, logLine);

            if (dl.Process!.HasExited)
            {
                try { dl.Process.WaitForExit(200); } catch { }
                DrainOutputQueue(dl, logLine);

                var exitCode = dl.Process.ExitCode;
                dl.Status = exitCode == 0 ? DlStatus.Complete : DlStatus.Error;
                if (dl.Status == DlStatus.Complete) { dl.Pct = 100; dl.ETA = "Done"; dl.Speed = "—"; }
                ClearDlProcess(dl);
                _activeCount = Math.Max(0, _activeCount - 1);
                startNext();
            }
        }
    }

    public void DecrementActive() => _activeCount = Math.Max(0, _activeCount - 1);

    // ── Browser / runtime checks ────────────────────────────────────────────────

    public static readonly Dictionary<string, string> ChromiumProcMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chrome"]   = "chrome",
        ["edge"]     = "msedge",
        ["brave"]    = "brave",
        ["opera"]    = "opera",
        ["vivaldi"]  = "vivaldi",
        ["chromium"] = "chromium"
    };

    public static bool IsBrowserRunning(string cookiesFrom)
    {
        var browser = cookiesFrom.Split(':')[0].ToLower().Trim();
        if (!ChromiumProcMap.TryGetValue(browser, out var proc)) return false;
        return Process.GetProcessesByName(proc).Length > 0;
    }

    public static bool IsChromiumBrowser(string cookiesFrom) =>
        ChromiumProcMap.ContainsKey(cookiesFrom.Split(':')[0].ToLower().Trim());

    public static bool IsRuntimeAvailable(string exe)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, "--version")
            { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            using var p = Process.Start(psi)!;
            p.WaitForExit(2000);
            return p.ExitCode == 0;
        }
        catch { return false; }
    }

    public static (bool found, string version) GetRuntimeVersion(string exe)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, "--version")
            { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            using var p = Process.Start(psi)!;
            var ver = p.StandardOutput.ReadLine() ?? p.StandardError.ReadLine() ?? "";
            p.WaitForExit(2000);
            return p.ExitCode == 0 ? (true, ver) : (false, "");
        }
        catch { return (false, ""); }
    }
}
