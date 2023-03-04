using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json.Nodes;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using ourMIPSSharp_App.Models;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class SettingsViewModel : ViewModelBase {
    #region Properties

    public MyAppTheme[] Themes { get; } = Enum.GetValues<MyAppTheme>();

    public ObservableCollection<CompilerMode> CompilerModes { get; } = new()
        { CompilerMode.OurMIPSSharp, CompilerMode.Philosonline, CompilerMode.Yapjoma, CompilerMode.Custom };

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

    private bool _isCheckedStrictNonDecimalNumbers;
    private bool _isCheckedStrictNonDecimalNumberLengths;
    private bool _isCheckedStrictDecimalNumberLengths;
    private bool _isCheckedStrictCaseSensitiveDescriptors;
    private bool _isCheckedStrictKeywordEndmacro;
    private bool _isCheckedStrictKeywordMend;
    private bool _isCheckedStrictNoColonAfterMacro;
    private bool _isCheckedStrictMacroDefinitionOrder;
    private bool _isCheckedStrictMacroArgumentNames;

    public bool IsCheckedStrictNonDecimalNumbers {
        get => _isCheckedStrictNonDecimalNumbers;
        set => this.RaiseAndSetIfChanged(ref _isCheckedStrictNonDecimalNumbers, value);
    }

    public bool IsCheckedStrictNonDecimalNumberLengths {
        get => _isCheckedStrictNonDecimalNumberLengths;
        set => this.RaiseAndSetIfChanged(ref _isCheckedStrictNonDecimalNumberLengths, value);
    }

    public bool IsCheckedStrictDecimalNumberLengths {
        get => _isCheckedStrictDecimalNumberLengths;
        set => this.RaiseAndSetIfChanged(ref _isCheckedStrictDecimalNumberLengths, value);
    }

    public bool IsCheckedStrictCaseSensitiveDescriptors {
        get => _isCheckedStrictCaseSensitiveDescriptors;
        set => this.RaiseAndSetIfChanged(ref _isCheckedStrictCaseSensitiveDescriptors, value);
    }

    public bool IsCheckedStrictKeywordEndmacro {
        get => _isCheckedStrictKeywordEndmacro;
        set => this.RaiseAndSetIfChanged(ref _isCheckedStrictKeywordEndmacro, value);
    }

    public bool IsCheckedStrictKeywordMend {
        get => _isCheckedStrictKeywordMend;
        set => this.RaiseAndSetIfChanged(ref _isCheckedStrictKeywordMend, value);
    }

    public bool IsCheckedStrictNoColonAfterMacro {
        get => _isCheckedStrictNoColonAfterMacro;
        set => this.RaiseAndSetIfChanged(ref _isCheckedStrictNoColonAfterMacro, value);
    }

    public bool IsCheckedStrictMacroDefinitionOrder {
        get => _isCheckedStrictMacroDefinitionOrder;
        set => this.RaiseAndSetIfChanged(ref _isCheckedStrictMacroDefinitionOrder, value);
    }

    public bool IsCheckedStrictMacroArgumentNames {
        get => _isCheckedStrictMacroArgumentNames;
        set => this.RaiseAndSetIfChanged(ref _isCheckedStrictMacroArgumentNames, value);
    }

    private DialectOptions _options;

    public DialectOptions Options {
        get => _options;
        private set => this.RaiseAndSetIfChanged(ref _options, value);
    }

    private bool _isApplyingMode;

    public bool IsApplyingMode {
        get => _isApplyingMode;
        private set => this.RaiseAndSetIfChanged(ref _isApplyingMode, value);
    }

    private ObservableAsPropertyHelper<bool> _isChanged;
    public bool IsChanged => _isChanged.Value;

    public MainViewModel Main { get; }
    public AppSettings Model { get; }

    public string VersionInfo => $"OurMIPSSharp Dev Build.\t" +
                                 $"Version {ThisAssembly.Git.SemVerString} " +
                                 (ThisAssembly.Git.IsDirty ? "(Dirty)" : "(Clean)") +
                                 $"\tCommit Date: {ThisAssembly.Git.CommitDate} " +
                                 $"\tLatest version is {GetLatestVersion()}";

    #endregion

    public SettingsViewModel(AppSettings model, MainViewModel main) {
        Main = main;
        Model = model;
        SelectedAppTheme = model.ActiveTheme.GetValueOrDefault();
        ApplyMode((DialectOptions)model.DialectOpts.GetValueOrDefault());

        this.WhenAnyValue(x => x.SelectedAppTheme)
            .Subscribe(t => model.ActiveTheme = t);

        this.WhenAnyValue(x => x.SelectedCompilerMode)
            .Subscribe(s => {
                if (s == CompilerMode.Custom) return;
                if (CompilerModes.Contains(CompilerMode.Custom))
                    CompilerModes.Remove(CompilerMode.Custom);
                ApplyMode(s.ToDialectOptions());
            });
        this.WhenAnyValue(
            x => x.IsCheckedStrictNonDecimalNumbers,
            x => x.IsCheckedStrictNonDecimalNumberLengths,
            x => x.IsCheckedStrictDecimalNumberLengths,
            x => x.IsCheckedStrictCaseSensitiveDescriptors,
            x => x.IsCheckedStrictKeywordEndmacro,
            x => x.IsCheckedStrictKeywordMend,
            x => x.IsCheckedStrictNoColonAfterMacro,
            x => x.IsCheckedStrictMacroDefinitionOrder,
            x => x.IsCheckedStrictMacroArgumentNames,
            (_, _, _, _, _, _, _, _, _) => ReadMode()
        ).Subscribe();

        this.WhenAnyValue(
                x => x.Main.LastBuildAttempt, // Just for build notifications
                x => x.Main.DebugSession,
                x => x.Options,
                (a, d, o2) =>
                    a is not null && d?.Backend.DialectOpts != null && d.Backend.DialectOpts.Value != o2)
            .ToProperty(this, x => x.IsChanged, out _isChanged);
    }

    private CompilerMode ReadMode() {
        if (IsApplyingMode) return SelectedCompilerMode;

        var opts = 0;
        if (IsCheckedStrictNonDecimalNumbers) opts ^= (int)DialectOptions.StrictNonDecimalNumbers;
        if (IsCheckedStrictNonDecimalNumberLengths) opts ^= (int)DialectOptions.StrictNonDecimalNumberLengths;
        if (IsCheckedStrictDecimalNumberLengths) opts ^= (int)DialectOptions.StrictDecimalNumberLengths;
        if (IsCheckedStrictCaseSensitiveDescriptors) opts ^= (int)DialectOptions.StrictCaseSensitiveDescriptors;
        if (IsCheckedStrictKeywordEndmacro) opts ^= (int)DialectOptions.StrictKeywordEndmacro;
        if (IsCheckedStrictKeywordMend) opts ^= (int)DialectOptions.StrictKeywordMend;
        if (IsCheckedStrictNoColonAfterMacro) opts ^= (int)DialectOptions.StrictNoColonAfterMacro;
        if (IsCheckedStrictMacroDefinitionOrder) opts ^= (int)DialectOptions.StrictMacroDefinitionOrder;
        if (IsCheckedStrictMacroArgumentNames) opts ^= (int)DialectOptions.StrictMacroArgumentNames;

        Model.DialectOpts = opts;
        Options = (DialectOptions)opts;
        UpdateModeSafe(Options.ToCompilerMode());

        return SelectedCompilerMode;
    }

    private void UpdateModeSafe(CompilerMode mode) {
        if (mode == CompilerMode.Custom && !CompilerModes.Contains(CompilerMode.Custom))
            CompilerModes.Add(CompilerMode.Custom);
        SelectedCompilerMode = mode;
    }

    private void ApplyMode(DialectOptions options) {
        IsApplyingMode = true;
        Options = options;
        Model.DialectOpts = (int)options;
        SelectedCompilerMode = Options.ToCompilerMode();

        // if (SelectedCompilerMode == CompilerMode.Custom && !CompilerModes.Contains(CompilerMode.Custom))
        //     CompilerModes.Add(CompilerMode.Custom);
        // if (SelectedCompilerMode != CompilerMode.Custom && CompilerModes.Contains(CompilerMode.Custom))
        //     CompilerModes.Remove(CompilerMode.Custom);

        IsCheckedStrictNonDecimalNumbers = options.HasFlag(DialectOptions.StrictNonDecimalNumbers);
        IsCheckedStrictNonDecimalNumberLengths = options.HasFlag(DialectOptions.StrictNonDecimalNumberLengths);
        IsCheckedStrictDecimalNumberLengths = options.HasFlag(DialectOptions.StrictDecimalNumberLengths);
        IsCheckedStrictCaseSensitiveDescriptors = options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors);
        IsCheckedStrictKeywordEndmacro = options.HasFlag(DialectOptions.StrictKeywordEndmacro);
        IsCheckedStrictKeywordMend = options.HasFlag(DialectOptions.StrictKeywordMend);
        IsCheckedStrictNoColonAfterMacro = options.HasFlag(DialectOptions.StrictNoColonAfterMacro);
        IsCheckedStrictMacroDefinitionOrder = options.HasFlag(DialectOptions.StrictMacroDefinitionOrder);
        IsCheckedStrictMacroArgumentNames = options.HasFlag(DialectOptions.StrictMacroArgumentNames);
        IsApplyingMode = false;
    }

    private static string GetLatestVersion() {
        Console.Error.WriteLine("Trying to get version");
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("User-Agent", "OurMIPS Version Info");
        var task = client.GetStringAsync("https://dl.lkekz.de/ourmips/version.json");
        try {
            if (!task.Wait(TimeSpan.FromSeconds(10)))
                return "unknown (timeout)";
            var response = task.Result;
            
            Console.Error.WriteLine("response: " + response);
            var root = JsonNode.Parse(response);
            return root["branches"][ThisAssembly.Git.Branch].AsValue().GetValue<string>();
        }
        catch (Exception ex) { }

        return "unknown (error)";
    }
}