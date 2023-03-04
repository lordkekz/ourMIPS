using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Dock.Model.Controls;
using ourMIPSSharp_App.Models;
using ourMIPSSharp_App.ViewModels.Editor;
using ourMIPSSharp_App.ViewModels.Tools;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable {
    #region ToolVMs

    public CommandBarViewModel Commands { get; }
    public InstructionsViewModel Instructions { get; }
    public ProblemsViewModel Problems { get; }
    public MemoryViewModel Memory { get; }
    public RegistersViewModel Registers { get; }

    #endregion

    private DocumentViewModel? _lastFile;
    private DocumentViewModel? _currentFile;
    private ApplicationState _state = ApplicationState.None;
    private readonly ObservableAsPropertyHelper<bool> _isDebugging;
    private readonly ObservableAsPropertyHelper<bool> _isEmulatorActive;
    private SettingsViewModel? _settings;
    private MemoryInitViewModel? _memoryInit;
    private ConsoleViewModelToolWrapper _consoleWrapper;
    private DebugSessionViewModel? _debugSession;
    private bool _isSettingsOpened;
    private bool _isMemoryInitOpened;
    private DateTime? _lastBuildAttempt;

    public DocumentViewModel? CurrentFile {
        get => _currentFile;
        set => this.RaiseAndSetIfChanged(ref _currentFile, value);
    }

    public ApplicationState State {
        get => _state;
        set => this.RaiseAndSetIfChanged(ref _state, value);
    }

    public bool IsDebugging => _isDebugging.Value;

    public bool IsEmulatorActive => _isEmulatorActive.Value;
    public IObservable<bool> IsEmulatorActiveObservable { get; }


    public SettingsViewModel? Settings {
        get => _settings;
        private set => this.RaiseAndSetIfChanged(ref _settings, value);
    }

    public MemoryInitViewModel? MemoryInit {
        get => _memoryInit;
        private set => this.RaiseAndSetIfChanged(ref _memoryInit, value);
    }

    public ConsoleViewModelToolWrapper ConsoleWrapper {
        get => _consoleWrapper;
        private set => this.RaiseAndSetIfChanged(ref _consoleWrapper, value);
    }

    public DebugSessionViewModel? DebugSession {
        get => _debugSession;
        set => this.RaiseAndSetIfChanged(ref _debugSession, value);
    }

    public bool IsSettingsOpened {
        get => _isSettingsOpened;
        set => this.RaiseAndSetIfChanged(ref _isSettingsOpened, value);
    }

    public bool IsMemoryInitOpened {
        get => _isMemoryInitOpened;
        set => this.RaiseAndSetIfChanged(ref _isMemoryInitOpened, value);
    }

    public DateTime? LastBuildAttempt {
        get => _lastBuildAttempt;
        set => this.RaiseAndSetIfChanged(ref _lastBuildAttempt, value);
    }

    public ObservableCollection<DocumentViewModel> OpenFiles { get; } = new();

    /// <summary>
    /// Fired whenever a new file is opened.
    /// </summary>
    public event EventHandler<DocumentViewModel>? FileOpened;

    private readonly List<IDisposable> _disposables = new();

    private readonly DockFactory? _factory;
    private IRootDock? _layout;

    public IRootDock? Layout {
        get => _layout;
        set => this.RaiseAndSetIfChanged(ref _layout, value);
    }

    public MainViewModel(AppSettings settings) : this() {
        Settings = new SettingsViewModel(settings, this);
    }

    public MainViewModel() {
        MemoryInit = new MemoryInitViewModel(this);
        Commands = new CommandBarViewModel(this);
        Instructions = new InstructionsViewModel(this);
        Problems = new ProblemsViewModel(this);
        Memory = new MemoryViewModel(this);
        Registers = new RegistersViewModel(this);
        ConsoleWrapper = new ConsoleViewModelToolWrapper(this);

        this.WhenAnyValue(x => x.State, s => s.IsDebuggerActive())
            .ToProperty(this, x => x.IsDebugging, out _isDebugging);

        IsEmulatorActiveObservable = this.WhenAnyValue(x => x.State,
            s => s.IsEmulatorActive());

        IsEmulatorActiveObservable.ToProperty(this, x => x.IsEmulatorActive, out _isEmulatorActive);

        _disposables.Add(IsEmulatorActiveObservable.Subscribe(b => {
            // TODO this is probably broken (or maybe not)
            if (ConsoleWrapper.ActiveConsole is not null) ConsoleWrapper.ActiveConsole.IsExpectingInput = false;
        }));

        _factory = new DockFactory(this, new object());
        _factory.DebugEvents();

        Layout = _factory?.CreateLayout();
        if (Layout is null) return;
        _factory?.InitLayout(Layout);
        Layout.Navigate.Execute("Documents");
    }

    public async Task OpenProgramFromSourceAsync(string sourceCode) {
        var f = new DocumentViewModel(this, $"Program {OpenFiles.Count}", sourceCode);
        f.Closing += async (sender, args) => {
            // Remove file if it closes
            OpenFiles.Remove(f);
            if (CurrentFile == f)
                CurrentFile = null;

            if (DebugSession == f.DebugSession) {
                await Commands.StopCommand.Execute();
                DebugSession = null;
            }

            State = OpenFiles.Count > 0 ? ApplicationState.FileOpened : ApplicationState.None;
        };
        OpenFiles.Add(f);
        CurrentFile = f;
        OnFileOpened();

        // await Task.Delay(250);
        await Commands.RebuildCommand.Execute();
    }

    protected virtual void OnFileOpened() {
        if (CurrentFile is null) return;

        if (State is ApplicationState.None)
            State = ApplicationState.FileOpened;
        FileOpened?.Invoke(this, CurrentFile);
    }

    private void Dispose(bool disposing) {
        if (!disposing) return;
        foreach (var d in _disposables) d.Dispose();
        _isEmulatorActive.Dispose();
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~MainViewModel() {
        Dispose(false);
    }
}