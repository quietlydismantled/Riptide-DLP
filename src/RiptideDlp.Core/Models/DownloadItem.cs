using System.Collections.Concurrent;
using System.Diagnostics;

namespace RiptideDlp.Core.Models;

public class DownloadItem
{
    public int      Id       { get; }
    public string   Url      { get; }
    public string   Title    { get; set; }
    public string   FilePath { get; set; } = "";
    public DlStatus Status   { get; set; } = DlStatus.Queued;
    public double   Pct      { get; set; }
    public string   Speed    { get; set; } = "—";
    public string   ETA      { get; set; } = "—";
    public string   Size     { get; set; } = "—";
    public string   LastLine { get; set; } = "";

    public Process?                        Process     { get; set; }
    public ConcurrentQueue<OutputRecord>   OutputQueue { get; set; } = new();
    public DataReceivedEventHandler?       OutHandler  { get; set; }
    public DataReceivedEventHandler?       ErrHandler  { get; set; }

    public DownloadItem(int id, string url)
    {
        Id    = id;
        Url   = url;
        Title = url;
    }
}
