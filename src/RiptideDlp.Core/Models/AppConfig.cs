using System.Text.Json;

namespace RiptideDlp.Core.Models;

public class AppConfig
{
    public int    Concurrent     { get; set; } = 3;
    public string OutputPath     { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    public string Format         { get; set; } = "bestvideo+bestaudio/best";
    public bool   AudioOnly      { get; set; } = false;
    public bool   NoOverwrites   { get; set; } = true;
    public bool   IgnoreErrors   { get; set; } = true;
    public bool   EmbedThumbnail { get; set; } = false;
    public bool   WriteSubtitles { get; set; } = false;
    public string SubLang        { get; set; } = "en";
    public bool   SponsorBlock   { get; set; } = false;
    public string RateLimit      { get; set; } = "";
    public string CookiesFrom    { get; set; } = "";
    public string CookieFile     { get; set; } = "";
    public string PlayerClient   { get; set; } = "";
    public string JsRuntime      { get; set; } = "node";
    public string ExtraArgs      { get; set; } = "";
    public bool   DarkMode       { get; set; } = true;
    public int[]  ColumnWidths   { get; set; } = [38, 380, 90, 165, 90, 65, 80];

    static readonly string CfgPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "riptide-dlp", "settings.json");

    static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(CfgPath))
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(CfgPath), Opts) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CfgPath)!);
            File.WriteAllText(CfgPath, JsonSerializer.Serialize(this, Opts));
        }
        catch { }
    }
}
