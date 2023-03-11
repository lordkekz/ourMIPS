#region

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ourMIPS_App.Models;
using ourMIPS_App.ViewModels;
using ourMIPS_App.Views;

#endregion

namespace ourMIPS_App;

public partial class App : Application {
    public new static App? Current => Application.Current as App;
    public AppSettings? Settings { get; private set; }

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        Settings = AppSettings.MakeInstance();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow {
                DataContext = new MainViewModel(Settings)
            };
            desktop.Exit += (_, _) => Settings?.SaveSettings();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform) {
            singleViewPlatform.MainView = new MainView {
                DataContext = new MainViewModel(Settings)
            };
            singleViewPlatform.MainView.Unloaded += (_, _) => Settings?.SaveSettings();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public IStorageProvider? GetStorageProvider() {
        return ApplicationLifetime switch {
            IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow?.StorageProvider,
            ISingleViewApplicationLifetime singleViewPlatform =>
                (singleViewPlatform.MainView?.Parent as TopLevel)?.StorageProvider,
            _ => null
        };
    }
}