using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ourMIPSSharp_App.Models;
using ourMIPSSharp_App.ViewModels;
using ourMIPSSharp_App.Views;

namespace ourMIPSSharp_App;

public partial class App : Application {
    public new static App Current => Application.Current as App;
    public AppSettings? Settings { get; private set; }

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        Settings = AppSettings.MakeInstance();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow {
                DataContext = new MainViewModel()
            };
            desktop.Exit += (_, _) => Settings?.SaveSettings();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform) {
            singleViewPlatform.MainView = new MainView {
                DataContext = new MainViewModel()
            };
            singleViewPlatform.MainView.Unloaded += (_, _) => Settings?.SaveSettings();
        }

        base.OnFrameworkInitializationCompleted();
    }
}