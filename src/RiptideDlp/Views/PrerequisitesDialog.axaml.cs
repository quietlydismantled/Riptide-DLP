using Avalonia.Controls;
using Avalonia.Interactivity;
using RiptideDlp.ViewModels;

namespace RiptideDlp.Views;

public partial class PrerequisitesDialog : Window
{
    public PrerequisitesDialog()
    {
        DataContext = new PrerequisitesViewModel();
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        BtnClose.Click += (_, _) => Close();
    }
}
