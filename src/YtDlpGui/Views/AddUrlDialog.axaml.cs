using Avalonia.Controls;
using Avalonia.Interactivity;
using YtDlpGui.ViewModels;

namespace YtDlpGui.Views;

public partial class AddUrlDialog : Window
{
    public AddUrlDialog()
    {
        InitializeComponent();
        DataContext = new AddUrlViewModel();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        BtnOk.Click     += (_, _) => Close(((AddUrlViewModel)DataContext!).ParseUrls());
        BtnCancel.Click += (_, _) => Close(null);
    }
}
