using CommunityToolkit.Mvvm.ComponentModel;

namespace YtDlpGui.ViewModels;

public partial class AddUrlViewModel : ViewModelBase
{
    [ObservableProperty] string _urlText = "";

    public IReadOnlyList<string> ParseUrls() =>
        UrlText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim())
            .Where(u => u.Contains("://"))
            .ToList();
}
