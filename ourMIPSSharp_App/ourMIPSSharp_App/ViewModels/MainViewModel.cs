using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using ourMIPSSharp_App.Models;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable {
    public static Interaction<Unit, IStorageFile?> SaveFileTo { get; } = new();
    public static Interaction<Unit, IStorageFile?> OpenProgramFile { get; } = new();
    public static Interaction<Unit, bool> AskSaveChanges { get; } = new();

    private OpenFileViewModel? _currentFile;

    public OpenFileViewModel? CurrentFile {
        get => _currentFile;
        private set => this.RaiseAndSetIfChanged(ref _currentFile, value);
    }


    private readonly ObservableAsPropertyHelper<FileBackend?> _currentBackend;
    public FileBackend? CurrentBackend => _currentBackend?.Value;

    private readonly ObservableAsPropertyHelper<ConsoleViewModel?> _currentConsole;
    public ConsoleViewModel? CurrentConsole => _currentConsole?.Value;

    private readonly ObservableAsPropertyHelper<EditorViewModel?> _currentEditor;
    public EditorViewModel? CurrentEditor => _currentEditor?.Value;

    private readonly ObservableAsPropertyHelper<DebuggerViewModel?> _currentDebugger;
    public DebuggerViewModel? CurrentDebugger => _currentDebugger?.Value;


    public ReactiveCommand<Unit, Unit> SettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> FileOpenCommand { get; }
    public ReactiveCommand<Unit, Unit> MemInitCommand { get; }
    public ReactiveCommand<Unit, Unit> RebuildCommand { get; }
    public ReactiveCommand<Unit, Unit> RunCommand { get; }
    public ReactiveCommand<Unit, Unit> DebugCommand { get; }
    public ReactiveCommand<Unit, Unit> StepCommand { get; }
    public ReactiveCommand<Unit, Unit> ForwardCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    private ApplicationState _state = ApplicationState.None;

    public ApplicationState State => _state;

    private readonly ObservableAsPropertyHelper<bool> _isEditorReadonly;
    public bool IsEditorReadonly => _isEditorReadonly.Value;

    private readonly ObservableAsPropertyHelper<bool> _isDebugging;
    public bool IsDebugging => _isDebugging.Value;

    private readonly ObservableAsPropertyHelper<bool> _isEmulatorActive;
    public bool IsEmulatorActive => _isEmulatorActive.Value;

    private SettingsViewModel _settings;

    public SettingsViewModel Settings {
        get => _settings;
        private set => this.RaiseAndSetIfChanged(ref _settings, value);
    }

    private bool _isSettingsOpened;

    public bool IsSettingsOpened {
        get => _isSettingsOpened;
        set => this.RaiseAndSetIfChanged(ref _isSettingsOpened, value);
    }

    public ObservableCollection<OpenFileViewModel> OpenFiles { get; } = new();

    /// <summary>
    /// Fired whenever a new file is opened.
    /// </summary>
    public event EventHandler<OpenFileViewModel>? FileOpened;

    /// <summary>
    /// Fired whenever a new file is brought to the foreground.
    /// </summary>
    public event EventHandler<OpenFileViewModel>? FileActivated;

    private readonly List<IDisposable> _disposables = new();

    public MainViewModel() {
        _disposables.Add(this.WhenAnyValue(x => x.CurrentFile).Subscribe(f => OnFileActivated()));

        var canExecuteNever = new[] { false }.ToObservable();
        var canExecuteAlways = new[] { true }.ToObservable();

        var isRebuildingAllowed = this.WhenAnyValue(x => x.State,
            s => s.IsRebuildingAllowed());
        var isEmulatorActive = this.WhenAnyValue(x => x.State,
            s => s.IsEmulatorActive());
        var isDebuggingButNotBusy = this.WhenAnyValue(x => x.State,
                x => x.CurrentFile.IsBackgroundBusy)
            .Select(t => t is { Item1: ApplicationState.Debugging, Item2: false });
        var isBuiltButEmulatorInactive = this.WhenAnyValue(x => x.State, s => s.IsBuilt() && !s.IsEmulatorActive());

        SettingsCommand = ReactiveCommand.Create(ExecuteSettingsCommand);
        FileOpenCommand = ReactiveCommand.CreateFromTask(ExecuteFileOpenCommand);
        MemInitCommand = ReactiveCommand.Create(() => throw new NotImplementedException(), canExecuteNever);
        RebuildCommand = ReactiveCommand.CreateFromTask(ExecuteRebuildCommand, isRebuildingAllowed);
        RunCommand = ReactiveCommand.CreateFromTask(ExecuteRunCommand, isBuiltButEmulatorInactive);
        DebugCommand = ReactiveCommand.CreateFromTask(ExecuteDebugCommand, isBuiltButEmulatorInactive);
        StepCommand = ReactiveCommand.CreateFromTask(ExecuteStepCommand, isDebuggingButNotBusy);
        ForwardCommand = ReactiveCommand.CreateFromTask(ExecuteForwardCommand, isDebuggingButNotBusy);
        StopCommand = ReactiveCommand.CreateFromTask(ExecuteStopCommand, isEmulatorActive);

        this.WhenAnyValue(x => x.State)
            .Select(x => !x.IsEditingAllowed())
            .ToProperty(this, x => x.IsEditorReadonly, out _isEditorReadonly);

        this.WhenAnyValue(x => x.State, s => s == ApplicationState.Debugging)
            .ToProperty(this, x => x.IsDebugging, out _isDebugging);

        isEmulatorActive.ToProperty(this, x => x.IsEmulatorActive, out _isEmulatorActive);

        _disposables.Add(isEmulatorActive.Subscribe(b => {
            if (CurrentConsole is not null) CurrentConsole.IsExpectingInput = false;
        }));

        // Current File Output Props
        this.WhenAnyValue(x => x.CurrentFile, f => f?.Backend)
            .ToProperty(this, x => x.CurrentBackend, out _currentBackend);
        this.WhenAnyValue(x => x.CurrentFile, f => f?.Console)
            .ToProperty(this, x => x.CurrentConsole, out _currentConsole);
        this.WhenAnyValue(x => x.CurrentFile, f => f?.Editor)
            .ToProperty(this, x => x.CurrentEditor, out _currentEditor);
        this.WhenAnyValue(x => x.CurrentFile, f => f?.Debugger)
            .ToProperty(this, x => x.CurrentDebugger, out _currentDebugger);
    }

    public void OpenProgramFromSource(string sourceCode) {
        var f = new OpenFileViewModel($"Untitled {OpenFiles.Count}");
        f.Editor.Document.Text = sourceCode;
        f.Closing += (sender, args) => {
            // Remove file if it closes
            OpenFiles.Remove(f);
            if (CurrentFile == f)
                CurrentFile = null;
        };
        OpenFiles.Add(f);
        CurrentFile = f;
        OnFileOpened();
    }

    private async Task ExecuteFileOpenCommand() {
        try {
            var file = await OpenProgramFile.Handle(Unit.Default);
            if (file is null) return;
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var sourceCode = await reader.ReadToEndAsync();

            OpenProgramFromSource(sourceCode);
        }
        catch (IOException ex) {
            Console.Error.WriteLine(ex);
        }
    }

    private void ExecuteSettingsCommand() {
        IsSettingsOpened = !IsSettingsOpened;
    }

    private async Task ExecuteRebuildCommand() {
        if (CurrentFile is null || CurrentFile!.IsBackgroundBusy) return;
        CurrentConsole?.Clear();
        CurrentFile.State = ApplicationState.Rebuilding;
        var str = CurrentEditor!.Text;
        await Task.Run(() => {
            CurrentBackend!.SourceCode = str;
            CurrentBackend.Rebuild();
            CurrentBackend.MakeEmulator();
        });

        await CurrentConsole!.FlushNewLines();
        if (CurrentBackend!.Ready) {
            CurrentFile.State = ApplicationState.Built;
            CurrentEditor.OnRebuilt(CurrentDebugger!.DebuggerBreakChangingObservable);
        }
        else CurrentFile.State = ApplicationState.FileOpened;
    }

    private async Task ExecuteRunCommand() {
        if (CurrentFile is null || CurrentFile!.IsBackgroundBusy || !CurrentBackend!.Ready) return;
        CurrentFile.IsBackgroundBusy = true;

        CurrentBackend.MakeEmulator();
        CurrentConsole!.Clear();
        CurrentFile.State = ApplicationState.Running;
        await Task.Run(CurrentDebugger!.DebuggerInstance.Run);
        CurrentFile.State = ApplicationState.Built;
        await CurrentConsole!.FlushNewLines();
        CurrentFile.IsBackgroundBusy = false;
    }

    private async Task ExecuteDebugCommand() {
        if (CurrentFile is null || CurrentFile!.IsBackgroundBusy || !CurrentBackend!.Ready) return;
        CurrentBackend.MakeEmulator();

        CurrentConsole!.Clear();
        CurrentDebugger.DebuggerInstance.StartSession();
        CurrentFile.State = ApplicationState.Debugging;
        await CurrentConsole!.FlushNewLines();
    }

    private async Task ExecuteStepCommand() {
        if (CurrentFile is null || CurrentFile!.IsBackgroundBusy) return;
        CurrentFile.IsBackgroundBusy = true;

        CurrentFile.State = ApplicationState.Running;
        await Task.Run(CurrentDebugger!.DebuggerInstance.Step);

        if (CurrentBackend!.CurrentEmulator!.Terminated || CurrentBackend.CurrentEmulator!.ErrorTerminated)
            CurrentFile.State = ApplicationState.Built;
        else
            CurrentFile.State = ApplicationState.Debugging;
        await CurrentConsole!.FlushNewLines();
        CurrentFile.IsBackgroundBusy = false;
    }

    private async Task ExecuteForwardCommand() {
        if (CurrentFile is null || CurrentFile!.IsBackgroundBusy) return;
        CurrentFile.IsBackgroundBusy = true;

        CurrentFile.State = ApplicationState.Running;
        await Task.Run(CurrentDebugger!.DebuggerInstance.Forward);

        if (CurrentBackend!.CurrentEmulator!.Terminated || CurrentBackend.CurrentEmulator!.ErrorTerminated)
            CurrentFile.State = ApplicationState.Built;
        else if (!CurrentBackend.CurrentEmulator!.ForceTerminated)
            CurrentFile.State = ApplicationState.Debugging;
        await CurrentConsole!.FlushNewLines();
        CurrentFile.IsBackgroundBusy = false;
    }

    private async Task ExecuteStopCommand() {
        if (CurrentBackend?.CurrentEmulator is null ||
            CurrentBackend.CurrentEmulator.EffectivelyTerminated) return;

        await CurrentDebugger!.StopEmulator();
        CurrentFile!.State = ApplicationState.Built;
        await CurrentConsole!.FlushNewLines();
    }

    protected virtual void OnFileOpened() {
        if (CurrentFile is null) return;

        var f = CurrentFile;
        f.WhenAnyValue(f => f.State).Subscribe(s => {
            if (CurrentFile == f)
                this.RaiseAndSetIfChanged(ref _state, s, nameof(State));
        });
        FileOpened?.Invoke(this, CurrentFile);
    }

    protected virtual void OnFileActivated() {
        foreach (var f in OpenFiles)
            if (f != CurrentFile)
                f.Debugger.DebuggerInstance.Hide();

        if (CurrentFile is null) return;

        CurrentDebugger?.DebuggerInstance.ComeBack();
        this.RaiseAndSetIfChanged(ref _state, CurrentFile.State, nameof(State));
        FileActivated?.Invoke(this, CurrentFile);
    }

    private void Dispose(bool disposing) {
        if (!disposing) return;
        foreach (var d in _disposables) d.Dispose();
        _currentBackend.Dispose();
        _currentConsole.Dispose();
        _currentEditor.Dispose();
        _currentDebugger.Dispose();
        _isEditorReadonly.Dispose();
        _isDebugging.Dispose();
        _isEmulatorActive.Dispose();
        SettingsCommand.Dispose();
        FileOpenCommand.Dispose();
        MemInitCommand.Dispose();
        RebuildCommand.Dispose();
        RunCommand.Dispose();
        DebugCommand.Dispose();
        StepCommand.Dispose();
        ForwardCommand.Dispose();
        StopCommand.Dispose();
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~MainViewModel() {
        Dispose(false);
    }
}