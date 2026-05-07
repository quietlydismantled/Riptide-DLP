using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using RiptideDlp.Core.Models;

namespace RiptideDlp.ViewModels;

public partial class DownloadItemViewModel : ViewModelBase
{
    public DownloadItem Model { get; }

    public int    Id  => Model.Id;
    public string Url => Model.Url;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressBar))]
    [NotifyPropertyChangedFor(nameof(StatusDisplay))]
    DlStatus _status = DlStatus.Queued;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    string _title = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressBar))]
    double _pct;

    [ObservableProperty] string _speed    = "—";
    [ObservableProperty] string _eta      = "—";
    [ObservableProperty] string _size     = "—";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusDisplay))]
    [NotifyPropertyChangedFor(nameof(Tooltip))]
    string _lastLine = "";

    public string ProgressBar => Status switch
    {
        DlStatus.Queued      => new string('─', 10) + "  0%",
        DlStatus.Complete    => new string('█', 10) + " 100%",
        DlStatus.Error       => "  Error  ",
        DlStatus.Cancelled   => "  Cancelled  ",
        DlStatus.Downloading =>
            new string('█', (int)(Pct / 10)) +
            new string('─', 10 - (int)(Pct / 10)) +
            $" {Pct:F1}%",
        _                    => new string('─', 10)
    };

    public string DisplayTitle => Title != Url
        ? Title
        : Url.Length > 90 ? Url[..87] + "..." : Url;

    public string StatusDisplay
    {
        get
        {
            if (Status == DlStatus.Downloading && !string.IsNullOrEmpty(LastLine))
            {
                var s = Regex.Replace(LastLine, @"^\[.+?\]\s*", "");
                return s.Length > 22 ? s[..19] + "..." : s;
            }
            return Status.ToString();
        }
    }

    public string Tooltip => string.IsNullOrEmpty(LastLine) ? Url : $"{LastLine}\n{Url}";

    public DownloadItemViewModel(DownloadItem model)
    {
        Model  = model;
        _title = model.Url;
    }

    public void Refresh()
    {
        Title    = Model.Title;
        Status   = Model.Status;
        Pct      = Model.Pct;
        Speed    = Model.Speed;
        Eta      = Model.ETA;
        Size     = Model.Size;
        LastLine = Model.LastLine;
    }
}
