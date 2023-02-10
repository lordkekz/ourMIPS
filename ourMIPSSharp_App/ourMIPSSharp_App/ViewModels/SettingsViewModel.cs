using System;
using System.Collections.ObjectModel;
using DynamicData.Binding;
using ourMIPSSharp_App.Models;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class SettingsViewModel : ViewModelBase {
    #region Properties

    public MyAppTheme[] Themes { get; } = Enum.GetValues<MyAppTheme>();
    public CompilerMode[] CompilerModes { get; } = Enum.GetValues<CompilerMode>();
    private MyAppTheme _selectedAppTheme;
    private CompilerMode _selectedCompilerMode;

    public MyAppTheme SelectedAppTheme {
        get => _selectedAppTheme;
        set => this.RaiseAndSetIfChanged(ref _selectedAppTheme, value);
    }

    public CompilerMode SelectedCompilerMode {
        get => _selectedCompilerMode;
        set => this.RaiseAndSetIfChanged(ref _selectedCompilerMode, value);
    }

    #endregion

    public SettingsViewModel(AppSettings model) {
        SelectedAppTheme = model.ActiveTheme.GetValueOrDefault();
        SelectedCompilerMode = model.ActiveCompilerMode.GetValueOrDefault();

        this.WhenAnyValue(x => x.SelectedAppTheme)
            .Subscribe(t => model.ActiveTheme = t);
        this.WhenAnyValue(x => x.SelectedCompilerMode)
            .Subscribe(t => model.ActiveCompilerMode = t);
    }
}