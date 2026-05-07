using Avalonia.Controls;
using Avalonia.Interactivity;
using RiptideDlp.ViewModels;

namespace RiptideDlp.Views;

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
