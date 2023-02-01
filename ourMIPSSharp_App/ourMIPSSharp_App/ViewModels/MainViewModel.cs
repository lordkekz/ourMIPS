using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using ourMIPSSharp_App.Models;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class MainViewModel : ViewModelBase {
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
    public ReactiveCommand<Unit, Unit> FileSaveCommand { get; }
    public ReactiveCommand<Unit, Unit> FileOpenCommand { get; }
    public ReactiveCommand<Unit, Unit> FileCloseCommand { get; }
    public ReactiveCommand<Unit, Unit> MemInitCommand { get; }
    public ReactiveCommand<Unit, Unit> RebuildCommand { get; }
    public ReactiveCommand<Unit, Unit> RunCommand { get; }
    public ReactiveCommand<Unit, Unit> DebugCommand { get; }
    public ReactiveCommand<Unit, Unit> StepCommand { get; }
    public ReactiveCommand<Unit, Unit> ForwardCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }


    private ApplicationState _state = ApplicationState.NotBuilt;

    public ApplicationState State {
        get => _state;
        set => this.RaiseAndSetIfChanged(ref _state, value);
    }

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

    /// <summary>
    /// Fired whenever a new file is opened.
    /// </summary>
    public event EventHandler<OpenFileViewModel?>? FileOpened;

    public MainViewModel() {
        var canExecuteNever = new[] { false }.ToObservable();
        var isRebuildingAllowed = this.WhenAnyValue(x => x.State,
            s => s.IsRebuildingAllowed());
        var isEmulatorActive = this.WhenAnyValue(x => x.State,
            s => s.IsEmulatorActive());
        var isDebuggingButNotBusy = this.WhenAnyValue(x => x.State,
                x => x.CurrentFile.IsBackgroundBusy)
            .Select(t => t is { Item1: ApplicationState.Debugging, Item2: false });
        var isBuiltButEmulatorInactive = this.WhenAnyValue(x => x.State, s => s.IsBuilt() && !s.IsEmulatorActive());

        SettingsCommand = ReactiveCommand.Create(ExecuteSettingsCommand);
        FileSaveCommand = ReactiveCommand.CreateFromTask(ExecuteFileSaveCommand);
        FileOpenCommand = ReactiveCommand.CreateFromTask(ExecuteFileOpenCommand);
        FileCloseCommand = ReactiveCommand.CreateFromTask(ExecuteFileCloseCommand);
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

        isEmulatorActive.Subscribe(b => {
            if (CurrentConsole is not null) CurrentConsole.IsExpectingInput = false;
        });

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

    public Interaction<Unit, IStorageFile?> SaveFileTo { get; } = new();
    public Interaction<Unit, IStorageFile?> OpenProgramFile { get; } = new();
    public Interaction<Unit, bool> AskSaveChanges { get; } = new();

    private async Task ExecuteFileCloseCommand() {
        if (CurrentEditor is { HasUnsavedChanges: false }) return;
        var saveChanges = await AskSaveChanges.Handle(Unit.Default);
        if (saveChanges)
            await ExecuteFileSaveCommand();
        CurrentFile = null;
    }

    private async Task ExecuteFileOpenCommand() {
        await ExecuteFileCloseCommand();
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

    public void OpenProgramFromSource(string sourceCode) {
        var f = new OpenFileViewModel($"Untitled {OpenFiles.Count}");
        f.Editor.Document.Text = sourceCode;
        OpenFiles.Add(f);
        CurrentFile = f;
        OnFileOpened();
    }

    public List<OpenFileViewModel> OpenFiles { get; } = new();

    private async Task ExecuteFileSaveCommand() {
        try {
            var file = await SaveFileTo.Handle(Unit.Default);
            if (file is null) return;
            await using var stream = await file.OpenWriteAsync();
            await using var write = new StreamWriter(stream);
            await write.WriteAsync(CurrentBackend.SourceCode);
            await write.FlushAsync();
        }
        catch (IOException ex) {
            Console.Error.WriteLine(ex);
        }
    }

    private void ExecuteSettingsCommand() {
        IsSettingsOpened = !IsSettingsOpened;
    }

    private async Task ExecuteRebuildCommand() {
        if (CurrentEditor is null) return;
        CurrentConsole?.Clear();
        State = ApplicationState.Rebuilding;
        var str = CurrentEditor.Text;
        await Task.Run(() => {
            CurrentBackend.SourceCode = str;
            CurrentBackend.Rebuild();
            CurrentBackend.MakeEmulator();
        });

        CurrentConsole?.FlushNewLines();
        if (CurrentBackend.Ready) {
            State = ApplicationState.Ready;
            CurrentEditor.OnRebuilt(CurrentDebugger.DebuggerBreakChangingObservable);
        }
        else State = ApplicationState.NotBuilt;
    }

    private async Task ExecuteRunCommand() {
        if (!CurrentBackend.Ready)
            return;
        if (CurrentFile.IsBackgroundBusy)
            await CurrentDebugger!.StopEmulator();
        CurrentFile.IsBackgroundBusy = true;

        CurrentBackend.MakeEmulator();
        CurrentConsole!.Clear();
        State = ApplicationState.Running;
        await Task.Run(CurrentDebugger!.DebuggerInstance.Run);
        State = ApplicationState.Ready;
        CurrentConsole.FlushNewLines();
        CurrentFile.IsBackgroundBusy = false;
    }

    private async Task ExecuteDebugCommand() {
        if (!CurrentBackend.Ready)
            return;
        if (CurrentFile.IsBackgroundBusy)
            await CurrentDebugger!.StopEmulator();
        CurrentBackend.MakeEmulator();


        CurrentConsole.Clear();
        CurrentBackend.TextInfoWriter.WriteLine("[EMULATOR] Debug session started.");
        State = ApplicationState.Debugging;
        CurrentConsole.FlushNewLines();
    }

    private async Task ExecuteStepCommand() {
        if (CurrentFile.IsBackgroundBusy) return;
        CurrentFile.IsBackgroundBusy = true;
        
        State = ApplicationState.Running;
        await Task.Run(CurrentDebugger!.DebuggerInstance.Step);

        if (CurrentBackend.CurrentEmulator!.Terminated || CurrentBackend.CurrentEmulator!.ErrorTerminated)
            State = ApplicationState.Ready;
        else
            State = ApplicationState.Debugging;
        CurrentConsole!.FlushNewLines();
        CurrentFile.IsBackgroundBusy = false;
    }

    private async Task ExecuteForwardCommand() {
        if (CurrentFile.IsBackgroundBusy) return;
        CurrentFile.IsBackgroundBusy = true;
        
        State = ApplicationState.Running;
        await Task.Run(CurrentDebugger!.DebuggerInstance.Forward);

        if (CurrentBackend.CurrentEmulator!.Terminated || CurrentBackend.CurrentEmulator!.ErrorTerminated)
            State = ApplicationState.Ready;
        else if (!CurrentBackend.CurrentEmulator!.ForceTerminated)
            State = ApplicationState.Debugging;
        CurrentConsole!.FlushNewLines();
        CurrentFile.IsBackgroundBusy = false;
    }

    private async Task ExecuteStopCommand() {
        if (CurrentBackend?.CurrentEmulator is null ||
            CurrentBackend.CurrentEmulator.EffectivelyTerminated) return;

        await CurrentDebugger!.StopEmulator();
        State = ApplicationState.Ready;
        CurrentConsole!.FlushNewLines();
    }

    protected virtual void OnFileOpened() {
        FileOpened?.Invoke(this, CurrentFile);
    }
}