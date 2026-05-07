using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using YtDlpGui.Core.Models;
using YtDlpGui.ViewModels;

namespace YtDlpGui.Views;

public partial class OptionsDialog : Window
{
    readonly AppConfig _existing;

    public OptionsDialog(AppConfig cfg)
    {
        _existing   = cfg;
        DataContext = new OptionsViewModel(cfg);
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        BtnSave.Click   += OnSave;
        BtnCancel.Click += (_, _) => Close(null);
        BtnBrowseOutput.Click += OnBrowseOutput;
        BtnBrowseCookie.Click += OnBrowseCookie;
    }

    void OnSave(object? sender, RoutedEventArgs e)
    {
        var vm = (OptionsViewModel)DataContext!;
        Close(vm.ToConfig(_existing));
    }

    async void OnBrowseOutput(object? sender, RoutedEventArgs e)
    {
        var vm = (OptionsViewModel)DataContext!;
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select output folder",
            AllowMultiple = false
        });
        if (folders.Count > 0)
            vm.OutputPath = folders[0].Path.LocalPath;
    }

    async void OnBrowseCookie(object? sender, RoutedEventArgs e)
    {
        var vm = (OptionsViewModel)DataContext!;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select cookies.txt (Netscape format)",
            AllowMultiple = false,
            FileTypeFilter = [new("Cookie files") { Patterns = ["*.txt"] }]
        });
        if (files.Count > 0)
            vm.CookieFile = files[0].Path.LocalPath;
    }
}
