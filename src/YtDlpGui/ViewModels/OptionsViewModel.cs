using CommunityToolkit.Mvvm.ComponentModel;
using YtDlpGui.Core.Models;
using YtDlpGui.Core.Services;

namespace YtDlpGui.ViewModels;

public partial class OptionsViewModel : ViewModelBase
{
    [ObservableProperty] int    _concurrent     = 3;
    [ObservableProperty] string _outputPath     = "";
    [ObservableProperty] string _format         = "bestvideo+bestaudio/best";
    [ObservableProperty] bool   _audioOnly;
    [ObservableProperty] bool   _noOverwrites   = true;
    [ObservableProperty] bool   _ignoreErrors   = true;
    [ObservableProperty] bool   _embedThumbnail;
    [ObservableProperty] bool   _writeSubtitles;
    [ObservableProperty] string _subLang        = "en";
    [ObservableProperty] bool   _sponsorBlock;
    [ObservableProperty] string _rateLimit      = "";
    [ObservableProperty] string _cookiesFrom    = "";
    [ObservableProperty] string _cookieFile     = "";
    [ObservableProperty] string _playerClient   = "";
    [ObservableProperty] string _jsRuntime      = "node";
    [ObservableProperty] string _extraArgs      = "";

    [ObservableProperty] string _cookieWarnText  = "";
    [ObservableProperty] string _cookieWarnColor = "#787878";
    [ObservableProperty] string _jsStatusText    = "";
    [ObservableProperty] string _jsStatusColor   = "#787878";

    public static readonly string[] FormatOptions =
    [
        "bestvideo+bestaudio/best",
        "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]",
        "best[height<=1080]",
        "best[height<=720]",
        "bestaudio/best"
    ];

    public static readonly string[] CookieOptions =
    [
        "", "chrome", "chrome:Default", "chrome:Profile 1",
        "firefox", "firefox:0", "edge", "edge:Default",
        "brave", "opera", "vivaldi", "safari"
    ];

    public static readonly string[] JsRuntimeOptions = ["", "node", "deno"];

    public static readonly string[] PlayerClientOptions =
        ["", "ios", "android", "mweb", "web", "tv_embedded", "web_creator"];

    public OptionsViewModel() { }

    public OptionsViewModel(AppConfig cfg)
    {
        Concurrent     = cfg.Concurrent;
        OutputPath     = cfg.OutputPath;
        Format         = cfg.Format;
        AudioOnly      = cfg.AudioOnly;
        NoOverwrites   = cfg.NoOverwrites;
        IgnoreErrors   = cfg.IgnoreErrors;
        EmbedThumbnail = cfg.EmbedThumbnail;
        WriteSubtitles = cfg.WriteSubtitles;
        SubLang        = cfg.SubLang;
        SponsorBlock   = cfg.SponsorBlock;
        RateLimit      = cfg.RateLimit;
        CookiesFrom    = cfg.CookiesFrom;
        CookieFile     = cfg.CookieFile;
        PlayerClient   = cfg.PlayerClient;
        JsRuntime      = cfg.JsRuntime;
        ExtraArgs      = cfg.ExtraArgs;
        UpdateCookieWarn();
        UpdateJsStatus();
    }

    partial void OnCookiesFromChanged(string value) => UpdateCookieWarn();
    partial void OnJsRuntimeChanged(string value)   => UpdateJsStatus();

    void UpdateCookieWarn()
    {
        var browser = CookiesFrom.Split(':')[0].ToLower().Trim();
        if (string.IsNullOrEmpty(CookiesFrom))
        {
            CookieWarnText  = "e.g. firefox, chrome, chrome:Default, edge";
            CookieWarnColor = "#787878";
        }
        else if (browser is "firefox" or "safari")
        {
            CookieWarnText  = "✓ OK: can stay open while downloading";
            CookieWarnColor = "#96DC82";
        }
        else if (DownloadService.IsBrowserRunning(CookiesFrom))
        {
            CookieWarnText  = $"⚠ {browser} is running — close it before downloading";
            CookieWarnColor = "#FF6450";
        }
        else
        {
            CookieWarnText  = "Must be fully closed when downloading";
            CookieWarnColor = "#FFA500";
        }
    }

    void UpdateJsStatus()
    {
        if (string.IsNullOrEmpty(JsRuntime))
        {
            JsStatusText  = "Default: deno only — some formats may be missing";
            JsStatusColor = "#787878";
        }
        else
        {
            var (found, ver) = DownloadService.GetRuntimeVersion(JsRuntime);
            if (found)
            {
                JsStatusText  = $"✓ Found: {ver}";
                JsStatusColor = "#96DC82";
            }
            else
            {
                JsStatusText  = "Not found on PATH — save to get install link";
                JsStatusColor = "#FF6450";
            }
        }
    }

    public AppConfig ToConfig(AppConfig existing) => new()
    {
        Concurrent     = Concurrent,
        OutputPath     = OutputPath.Trim(),
        Format         = Format.Trim(),
        AudioOnly      = AudioOnly,
        NoOverwrites   = NoOverwrites,
        IgnoreErrors   = IgnoreErrors,
        EmbedThumbnail = EmbedThumbnail,
        WriteSubtitles = WriteSubtitles,
        SubLang        = SubLang.Trim(),
        SponsorBlock   = SponsorBlock,
        RateLimit      = RateLimit.Trim(),
        CookiesFrom    = CookiesFrom.Trim(),
        CookieFile     = CookieFile.Trim(),
        PlayerClient   = PlayerClient.Trim(),
        JsRuntime      = JsRuntime.Trim(),
        ExtraArgs      = ExtraArgs.Trim(),
        DarkMode       = existing.DarkMode,
        ColumnWidths   = existing.ColumnWidths
    };
}
