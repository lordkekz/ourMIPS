using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Dock.Model.Controls;
using Dock.Model.Core;
using ourMIPSSharp_App.Models;
using ourMIPSSharp_App.ViewModels.Editor;
using ourMIPSSharp_App.ViewModels.Tools;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable {
    #region ToolVMs

    public CommandBarViewModel Commands { get; }
    public InstructionsViewModel Instructions { get; }
    public MemoryViewModel Memory { get; }
    public RegistersViewModel Registers { get; }

    #endregion


    private OpenFileViewModel? _lastFile;
    private OpenFileViewModel? _currentFile;

    public OpenFileViewModel? CurrentFile {
        get => _currentFile;
        set => this.RaiseAndSetIfChanged(ref _currentFile, value);
    }

    private readonly ObservableAsPropertyHelper<FileBackend?> _currentBackend;
    public FileBackend? CurrentBackend => _currentBackend?.Value;

    private readonly ObservableAsPropertyHelper<ConsoleViewModel?> _currentConsole;
    public ConsoleViewModel? CurrentConsole => _currentConsole?.Value;

    private readonly ObservableAsPropertyHelper<DocumentViewModel?> _currentEditor;
    public DocumentViewModel? CurrentEditor => _currentEditor?.Value;


    private ApplicationState _state = ApplicationState.None;

    public ApplicationState State => _state;


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
    public event EventHandler<FileSwitchedEventArgs>? FileSwitched;

    private readonly List<IDisposable> _disposables = new();

    private readonly IFactory? _factory;
    private IRootDock? _layout;

    public IRootDock? Layout {
        get => _layout;
        set => this.RaiseAndSetIfChanged(ref _layout, value);
    }

    public MainViewModel() {
        _disposables.Add(this.WhenAnyValue(x => x.CurrentFile).Subscribe(f => OnFileSwitched()));

        Commands = new CommandBarViewModel(this);
        Instructions = new InstructionsViewModel(this);
        Memory = new MemoryViewModel(this);
        Registers = new RegistersViewModel(this);


        this.WhenAnyValue(x => x.State, s => s == ApplicationState.Debugging)
            .ToProperty(this, x => x.IsDebugging, out _isDebugging);

        var isEmulatorActive = this.WhenAnyValue(x => x.State,
            s => s.IsEmulatorActive());

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


        _factory = new DockFactory(this, new object());
        DebugFactoryEvents(_factory);

        Layout = _factory?.CreateLayout();
        if (Layout is null) return;
        _factory?.InitLayout(Layout);
        Layout.Navigate.Execute("Documents");
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

    protected virtual void OnFileOpened() {
        if (CurrentFile is null) return;

        var f = CurrentFile;
        f.WhenAnyValue(f => f.State).Subscribe(s => {
            if (CurrentFile == f)
                this.RaiseAndSetIfChanged(ref _state, s, nameof(State));
        });
        FileOpened?.Invoke(this, CurrentFile);
    }

    protected virtual void OnFileSwitched() {
        foreach (var f in OpenFiles)
            if (f != CurrentFile)
                f.Editor.DebuggerInstance.Hide();

        _lastFile = CurrentFile;
        
        CurrentEditor?.DebuggerInstance.ComeBack();
        this.RaiseAndSetIfChanged(ref _state, CurrentFile?.State ?? ApplicationState.None, nameof(State));
        FileSwitched?.Invoke(this, new FileSwitchedEventArgs{DeactivatedFile = _lastFile, ActivatedFile = CurrentFile});
    }

    private void Dispose(bool disposing) {
        if (!disposing) return;
        foreach (var d in _disposables) d.Dispose();
        _currentBackend.Dispose();
        _currentConsole.Dispose();
        _currentEditor.Dispose();
        _isDebugging.Dispose();
        _isEmulatorActive.Dispose();
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~MainViewModel() {
        Dispose(false);
    }

    private void DebugFactoryEvents(IFactory factory)
    {
        factory.ActiveDockableChanged += (_, args) =>
        {
            Debug.WriteLine($"[ActiveDockableChanged] Title='{args.Dockable?.Title}'");
        };

        factory.FocusedDockableChanged += (_, args) =>
        {
            Debug.WriteLine($"[FocusedDockableChanged] Title='{args.Dockable?.Title}'");
        };

        factory.DockableAdded += (_, args) =>
        {
            Debug.WriteLine($"[DockableAdded] Title='{args.Dockable?.Title}'");
        };

        factory.DockableRemoved += (_, args) =>
        {
            Debug.WriteLine($"[DockableRemoved] Title='{args.Dockable?.Title}'");
        };

        factory.DockableClosed += (_, args) =>
        {
            Debug.WriteLine($"[DockableClosed] Title='{args.Dockable?.Title}'");
        };

        factory.DockableMoved += (_, args) =>
        {
            Debug.WriteLine($"[DockableMoved] Title='{args.Dockable?.Title}'");
        };

        factory.DockableSwapped += (_, args) =>
        {
            Debug.WriteLine($"[DockableSwapped] Title='{args.Dockable?.Title}'");
        };

        factory.DockablePinned += (_, args) =>
        {
            Debug.WriteLine($"[DockablePinned] Title='{args.Dockable?.Title}'");
        };

        factory.DockableUnpinned += (_, args) =>
        {
            Debug.WriteLine($"[DockableUnpinned] Title='{args.Dockable?.Title}'");
        };

        factory.WindowOpened += (_, args) =>
        {
            Debug.WriteLine($"[WindowOpened] Title='{args.Window?.Title}'");
        };

        factory.WindowClosed += (_, args) =>
        {
            Debug.WriteLine($"[WindowClosed] Title='{args.Window?.Title}'");
        };

        factory.WindowClosing += (_, args) =>
        {
            // NOTE: Set to True to cancel window closing.
#if false
                args.Cancel = true;
#endif
            Debug.WriteLine($"[WindowClosing] Title='{args.Window?.Title}', Cancel={args.Cancel}");
        };

        factory.WindowAdded += (_, args) =>
        {
            Debug.WriteLine($"[WindowAdded] Title='{args.Window?.Title}'");
        };

        factory.WindowRemoved += (_, args) =>
        {
            Debug.WriteLine($"[WindowRemoved] Title='{args.Window?.Title}'");
        };

        factory.WindowMoveDragBegin += (_, args) =>
        {
            // NOTE: Set to True to cancel window dragging.
#if false
                args.Cancel = true;
#endif
            Debug.WriteLine($"[WindowMoveDragBegin] Title='{args.Window?.Title}', Cancel={args.Cancel}, X='{args.Window?.X}', Y='{args.Window?.Y}'");
        };

        factory.WindowMoveDrag += (_, args) =>
        {
            Debug.WriteLine($"[WindowMoveDrag] Title='{args.Window?.Title}', X='{args.Window?.X}', Y='{args.Window?.Y}");
        };

        factory.WindowMoveDragEnd += (_, args) =>
        {
            Debug.WriteLine($"[WindowMoveDragEnd] Title='{args.Window?.Title}', X='{args.Window?.X}', Y='{args.Window?.Y}");
        };
    }
}

public class FileSwitchedEventArgs : EventArgs {
    public OpenFileViewModel? DeactivatedFile { get; init; }
    public OpenFileViewModel? ActivatedFile { get; init; }
}