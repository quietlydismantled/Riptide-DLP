using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RiptideDlp.Core.Services;

namespace RiptideDlp.ViewModels;

public partial class PrerequisiteEntryViewModel : ObservableObject
{
    public string  Exe         { get; }
    public string  DisplayName { get; }
    public string  WhyYouNeed  { get; }
    public string  GetItUrl    { get; }
    public bool    Required    { get; }
    public string  VersionFlag { get; }

    [ObservableProperty] bool   _isInstalled;
    [ObservableProperty] string _version = "—";

    public string StatusGlyph => IsInstalled ? "✓" : (Required ? "✗" : "○");
    public string StatusText  => IsInstalled ? $"installed (v{Version})"
                              : Required     ? "MISSING — required"
                                             : "not installed (optional)";

    partial void OnIsInstalledChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusGlyph));
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnVersionChanged(string value) => OnPropertyChanged(nameof(StatusText));

    [RelayCommand]
    void OpenLink()
    {
        try { Process.Start(new ProcessStartInfo(GetItUrl) { UseShellExecute = true }); }
        catch { }
    }

    public PrerequisiteEntryViewModel(string exe, string displayName, string whyYouNeed, string getItUrl,
                                       bool required, string versionFlag = "--version")
    {
        Exe = exe; DisplayName = displayName; WhyYouNeed = whyYouNeed; GetItUrl = getItUrl;
        Required = required; VersionFlag = versionFlag;
        Refresh();
    }

    public void Refresh()
    {
        var (found, ver) = DownloadService.GetRuntimeVersion(Exe, VersionFlag);
        IsInstalled = found;
        Version     = CleanVersion(ver);
    }

    static string CleanVersion(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "?";
        var s = raw.Trim();
        // ffmpeg prints "ffmpeg version N.N.N Copyright …" — keep just the number
        var m = System.Text.RegularExpressions.Regex.Match(s, @"version\s+(\S+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value;
        return s;
    }
}

public partial class PrerequisitesViewModel : ViewModelBase
{
    public ObservableCollection<PrerequisiteEntryViewModel> Entries { get; } =
    [
        new("yt-dlp", "yt-dlp",
            "The actual downloader. Without this, this whole app is just a really pretty list of links you can't do anything with.",
            "https://github.com/yt-dlp/yt-dlp/releases", required: true),

        new("ffmpeg", "FFmpeg",
            "Glues video and audio streams together (YouTube ships them separately, like IKEA furniture). Also handles subtitles, thumbnails, conversions, and roughly 1000 other things you'll appreciate eventually.",
            "https://www.gyan.dev/ffmpeg/builds/", required: true, versionFlag: "-version"),

        new("node", "Node.js",
            "YouTube hides some video URLs behind JavaScript puzzles. Node solves them. Without it, most videos still work, but the trickier ones will fail with a sad little error.",
            "https://nodejs.org/", required: false),
    ];

    public bool AnyRequiredMissing => Entries.Any(e => e.Required && !e.IsInstalled);

    [RelayCommand]
    void RefreshAll()
    {
        foreach (var e in Entries) e.Refresh();
        OnPropertyChanged(nameof(AnyRequiredMissing));
    }
}
