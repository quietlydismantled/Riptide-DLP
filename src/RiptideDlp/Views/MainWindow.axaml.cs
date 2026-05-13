using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using RiptideDlp.Core.Models;
using RiptideDlp.ViewModels;
using RiptideDlp.Views;

namespace RiptideDlp.Views;

public partial class MainWindow : Window
{
    MainWindowViewModel Vm => (MainWindowViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Apply initial theme from saved config
        Application.Current!.RequestedThemeVariant =
            Vm.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;

        // Wire ViewModel delegates
        Vm.RequestOpenFiles = async () =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select URL/text files",
                AllowMultiple = true,
                FileTypeFilter =
                [
                    new("All files")              { Patterns = ["*"] },
                    new("URL / Text / List files") { Patterns = ["*.url", "*.txt", "*.lst"] },
                ]
            });
            return files.Select(f => f.Path.LocalPath).ToList();
        };

        Vm.RequestAddUrl = async () =>
        {
            var dlg    = new AddUrlDialog();
            var result = await dlg.ShowDialog<IReadOnlyList<string>?>(this);
            if (result == null || result.Count == 0) return null;
            return string.Join("\n", result);
        };

        Vm.RequestPaste = async () =>
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            return clipboard == null ? null : await clipboard.TryGetTextAsync();
        };

        Vm.RequestCopyText = text =>
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null && text != null) _ = clipboard.SetTextAsync(text);
        };

        Vm.RequestConfirm = async (message, title) =>
        {
            var box = new Window
            {
                Title                 = title,
                Width                 = 420,
                SizeToContent         = SizeToContent.Height,
                CanResize             = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            var tcs = new TaskCompletionSource<bool>();
            var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
            panel.Children.Add(new TextBlock
            {
                Text         = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            });
            var btns = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
            var btnYes = new Button { Content = "Yes", Width = 70 };
            var btnNo  = new Button { Content = "No",  Width = 70 };
            btnYes.Click += (_, _) => { tcs.TrySetResult(true);  box.Close(); };
            btnNo.Click  += (_, _) => { tcs.TrySetResult(false); box.Close(); };
            btns.Children.Add(btnYes);
            btns.Children.Add(btnNo);
            panel.Children.Add(btns);
            box.Content = panel;
            _ = box.ShowDialog(this);
            return await tcs.Task;
        };

        Vm.RequestOptions = async cfg =>
        {
            var dlg = new OptionsDialog(cfg);
            return await dlg.ShowDialog<AppConfig?>(this);
        };

        // Restore saved column widths
        var widths = Vm.GetColumnWidths();
        var cols   = DownloadGrid.Columns;
        for (int i = 0; i < Math.Min(widths.Length, cols.Count); i++)
            if (widths[i] > 10) cols[i].Width = new DataGridLength(widths[i]);

        // DragDrop — AllowDrop on both Window and DropZone so OS (Explorer) drops hit either
        DragDrop.SetAllowDrop(this,     true);
        DragDrop.SetAllowDrop(DropZone, true);
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent,      OnDrop);
        DropZone.PointerPressed += OnDropZoneClick;

        // DataGrid keyboard
        DownloadGrid.KeyDown += OnGridKeyDown;

        // Context menu + double-click — built in code-behind so commands get the actual row item
        DownloadGrid.AddHandler(PointerPressedEvent, OnGridPointerPressed, handledEventsToo: true);
        DownloadGrid.DoubleTapped += (_, _) =>
        {
            if (DownloadGrid.SelectedItem is DownloadItemViewModel item)
                Vm.OpenFileCommand.Execute(item);
        };
        DownloadGrid.ContextMenu = BuildRowContextMenu();

        // Scroll log to bottom when new lines arrive (uses the filtered view bound to ListBox)
        Vm.FilteredLogLines.CollectionChanged += (_, _) =>
        {
            if (Vm.IsConsoleVisible && Vm.FilteredLogLines.Count > 0)
                LogListBox.ScrollIntoView(Vm.FilteredLogLines.LastOrDefault()!);
        };

        // Expand/collapse the console row when toggled
        ApplyConsoleRowHeight();
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsConsoleVisible))
                ApplyConsoleRowHeight();
        };

        Closing += (_, _) =>
        {
            var savedWidths = DownloadGrid.Columns.Select(c => (int)c.ActualWidth).ToArray();
            Vm.SaveColumnWidths(savedWidths);
            Vm.OnClosing();
        };

        // Startup prereq check — open the dialog if anything required is missing
        _ = ShowPrereqsIfMissingAsync();
    }

    // ── DragDrop ──────────────────────────────────────────────────────────────

    void OnDragEnter(object? s, DragEventArgs e)
    {
        DropZone.Background = Application.Current?.FindResource("BrushDropHover") as Avalonia.Media.IBrush;
        e.DragEffects = (e.DataTransfer.Contains(DataFormat.File) || e.DataTransfer.Contains(DataFormat.Text))
            ? DragDropEffects.Copy : DragDropEffects.None;
    }

    void OnDragLeave(object? s, DragEventArgs e)
        => DropZone.Background = Application.Current?.FindResource("BrushPanel") as Avalonia.Media.IBrush;

    void OnDrop(object? s, DragEventArgs e)
    {
        DropZone.Background = Application.Current?.FindResource("BrushPanel") as Avalonia.Media.IBrush;
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            var paths = e.DataTransfer.TryGetFiles()?.Select(f => f.Path.LocalPath).ToList() ?? [];
            Vm.AddUrlsFromDrop(paths);
        }
        else if (e.DataTransfer.Contains(DataFormat.Text))
        {
            var text = e.DataTransfer.TryGetText();
            if (!string.IsNullOrWhiteSpace(text)) Vm.AddUrlsFromText(text);
        }
    }

    async void OnDropZoneClick(object? s, PointerPressedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select URL/text files",
            AllowMultiple = true,
            FileTypeFilter =
                [
                    new("All files")              { Patterns = ["*"] },
                    new("URL / Text / List files") { Patterns = ["*.url", "*.txt", "*.lst"] },
                ]
        });
        if (files.Count > 0)
            Vm.AddUrlsFromDrop(files.Select(f => f.Path.LocalPath).ToList());
    }

    // ── Grid keyboard shortcuts ───────────────────────────────────────────────

    void OnGridKeyDown(object? s, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            foreach (var item in DownloadGrid.SelectedItems.Cast<DownloadItemViewModel>().ToList())
                Vm.RemoveItemCommand.Execute(item);
            e.Handled = true;
        }
    }

    // ── Console row sizing ────────────────────────────────────────────────────

    GridLength _lastConsoleHeight = new(180);

    void ApplyConsoleRowHeight()
    {
        if (Vm.IsConsoleVisible)
            ContentGrid.RowDefinitions[2].Height = _lastConsoleHeight.Value > 0 ? _lastConsoleHeight : new GridLength(180);
        else
        {
            if (ContentGrid.RowDefinitions[2].Height.Value > 0) _lastConsoleHeight = ContentGrid.RowDefinitions[2].Height;
            ContentGrid.RowDefinitions[2].Height = new GridLength(0);
        }
    }

    // ── DataGrid context menu ─────────────────────────────────────────────────

    void OnGridPointerPressed(object? s, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsRightButtonPressed) return;
        var source = e.Source as Control;
        while (source != null && source is not DataGridRow)
            source = source.Parent as Control;
        if (source is DataGridRow row && row.DataContext is DownloadItemViewModel item)
            DownloadGrid.SelectedItem = item;
    }

    ContextMenu BuildRowContextMenu()
    {
        DownloadItemViewModel? Row() => DownloadGrid.SelectedItem as DownloadItemViewModel;
        MenuItem Item(string header, Action<DownloadItemViewModel> act)
        {
            var mi = new MenuItem { Header = header };
            mi.Click += (_, _) => { if (Row() is { } item) act(item); };
            return mi;
        }
        return new ContextMenu
        {
            Items =
            {
                Item("Open file",           item => Vm.OpenFileCommand.Execute(item)),
                Item("Open output folder",  item => Vm.OpenItemFolderCommand.Execute(item)),
                new Separator(),
                Item("Cancel",              item => Vm.CancelItemCommand.Execute(item)),
                Item("Retry",               item => Vm.RetryItemCommand.Execute(item)),
                Item("Remove",              item => Vm.RemoveItemCommand.Execute(item)),
                new Separator(),
                Item("Copy URL",            item => Vm.CopyUrlCommand.Execute(item)),
            }
        };
    }

    // ── About dialog ─────────────────────────────────────────────────────────

    async void OnAboutClick(object? s, RoutedEventArgs e)
    {
        if (Vm.RequestConfirm != null)
            await Vm.RequestConfirm(
                "Riptide DLP\n\nA drag-and-drop GUI wrapper for yt-dlp.\nDrop .url / .txt / .lst files or paste URLs to start downloading.",
                "About Riptide DLP");
    }

    // ── Prerequisites dialog ─────────────────────────────────────────────────

    async void OnPrereqsClick(object? s, RoutedEventArgs e)
    {
        var dlg = new PrerequisitesDialog();
        await dlg.ShowDialog(this);
    }

    async Task ShowPrereqsIfMissingAsync()
    {
        var vm = new PrerequisitesViewModel();
        if (!vm.AnyRequiredMissing) return;
        var dlg = new PrerequisitesDialog();
        await dlg.ShowDialog(this);
    }
}