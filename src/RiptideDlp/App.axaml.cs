using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using RiptideDlp.Core.Models;
using RiptideDlp.ViewModels;
using RiptideDlp.Views;

namespace RiptideDlp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var cfg = AppConfig.Load();
            RequestedThemeVariant = cfg.DarkMode ? ThemeVariant.Dark : ThemeVariant.Light;

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}