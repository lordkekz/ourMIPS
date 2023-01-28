using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using ourMIPSSharp_App.Models;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class MainViewModel : ViewModelBase {
    public OpenScriptBackend Backend { get; private set; }
    public ObservableCollection<InstructionEntry> InstructionList { get; } = new();
    public ObservableCollection<RegisterEntry> RegisterList { get; } = new();
    public ObservableCollection<MemoryEntry> MemoryList { get; } = new();

    /// <summary>
    /// Reserved for background thread whenever emulator is executing.
    /// </summary>
    public List<Breakpoint> Breakpoints { get; } = new();

    /// <summary>
    /// Reserved for UI Thread.
    /// </summary>
    public List<Breakpoint> UIBreakpoints { get; } = new();


    private bool _isBackgroundBusy;

    public bool IsBackgroundBusy {
        get => _isBackgroundBusy;
        set => this.RaiseAndSetIfChanged(ref _isBackgroundBusy, value);
    }

    private ConsoleViewModel? _console;

    public ConsoleViewModel? Console {
        get => _console;
        set => this.RaiseAndSetIfChanged(ref _console, value);
    }

    private int _overflowFlag;

    public int OverflowFlag {
        get => _overflowFlag;
        set => this.RaiseAndSetIfChanged(ref _overflowFlag, value);
    }

    private int _programCounter;

    public int ProgramCounter {
        get => _programCounter;
        set => this.RaiseAndSetIfChanged(ref _programCounter, value);
    }

    public ReactiveCommand<Unit, Unit> SettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> MemInitCommand { get; }
    public ReactiveCommand<Unit, Unit> RebuildCommand { get; }
    public ReactiveCommand<Unit, Unit> RunCommand { get; }
    public ReactiveCommand<Unit, Unit> DebugCommand { get; }
    public ReactiveCommand<Unit, Unit> StepCommand { get; }
    public ReactiveCommand<Unit, Unit> ForwardCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    public TextDocument Document { get; }

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

    private string _editorCaretInfo;

    public string EditorCaretInfo {
        get => _editorCaretInfo;
        private set => this.RaiseAndSetIfChanged(ref _editorCaretInfo, value);
    }

    /// <summary>
    /// Fired whenever the debugger enters its break mode through a breakpoint, manual stepping or a pause command.
    /// </summary>
    public event EventHandler<DebuggerBreakEventHandlerArgs>? DebuggerBreaking;

    /// <summary>
    /// Fired whenever the debugger leaves its break mode by continuing execution or terminating.
    /// </summary>
    public event EventHandler<DebuggerBreakEventHandlerArgs>? DebuggerBreakEnding;

    /// <summary>
    /// Fired whenever the program was rebuilt.
    /// </summary>
    public event EventHandler? Rebuilt;

    /// <summary>
    /// Fired whenever the debugger register/memory are to be updated.
    /// </summary>
    public event EventHandler<DebuggerUpdatingEventHandlerArgs>? DebuggerUpdating;

    private readonly IObservable<EventPattern<DebuggerUpdatingEventHandlerArgs>> _debuggerUpdatingObservable;
    private readonly IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> _debuggerBreakingObservable;
    private readonly IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> _debuggerBreakEndingObservable;
    private readonly IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> _debuggerBreakChangingObservable;

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

    public MainViewModel() {
        var canExecuteNever = new[] { false }.ToObservable();
        var isRebuildingAllowed = this.WhenAnyValue(x => x.State,
            s => s.IsRebuildingAllowed());
        var isEmulatorActive = this.WhenAnyValue(x => x.State,
            s => s.IsEmulatorActive());
        var isDebuggingButNotBusy = this.WhenAnyValue(x => x.State,
                x => x.IsBackgroundBusy)
            .Select(t => t is { Item1: ApplicationState.Debugging, Item2: false });
        var isBuiltButEmulatorInactive = this.WhenAnyValue(x => x.State, s => s.IsBuilt() && !s.IsEmulatorActive());
        SettingsCommand = ReactiveCommand.Create(ExecuteSettingsCommand);
        SaveCommand = ReactiveCommand.Create(() => throw new NotImplementedException(), canExecuteNever);
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
            if (Console is not null) Console.IsExpectingInput = false;
        });

        _debuggerUpdatingObservable = Observable.FromEventPattern<DebuggerUpdatingEventHandlerArgs>(
            a => DebuggerUpdating += a,
            a => DebuggerUpdating -= a);

        _debuggerBreakingObservable = Observable.FromEventPattern<DebuggerBreakEventHandlerArgs>(
            a => DebuggerBreaking += a,
            a => DebuggerBreaking -= a);

        _debuggerBreakEndingObservable = Observable.FromEventPattern<DebuggerBreakEventHandlerArgs>(
            a => DebuggerBreakEnding += a,
            a => DebuggerBreakEnding -= a);

        _debuggerBreakChangingObservable = _debuggerBreakingObservable.Merge(_debuggerBreakEndingObservable);

        // Load mult_philos sample from unit tests
        Backend = new OpenScriptBackend(
            "../../../../../lib_ourMIPSSharp_Tests/Samples/instructiontests_philos.ourMIPS");
        Document = new TextDocument(Backend.SourceCode);
        Console = new ConsoleViewModel(Backend);

        // Init RegisterList
        for (var i = 0; i < 32; i++) {
            RegisterList.Add(new RegisterEntry((Register)i, () => Backend.CurrentEmulator!.Registers,
                _debuggerUpdatingObservable));
        }
    }

    private void ExecuteSettingsCommand() {
        IsSettingsOpened = !IsSettingsOpened;
    }

    private async Task ExecuteRebuildCommand() {
        Console.Clear();
        State = ApplicationState.Rebuilding;
        var str = Document.Text;
        await Task.Run(() => {
            Backend.SourceCode = str;
            Backend.Rebuild();
        });

        Console.FlushNewLines();
        if (Backend.Ready) {
            State = ApplicationState.Ready;
            OnRebuilt();
        }
        else State = ApplicationState.NotBuilt;
    }

    private void UpdateBreakpoints() {
        foreach (var bp in UIBreakpoints.Where(bp => !Breakpoints.Contains(bp)))
            Breakpoints.Add(bp);
        foreach (var bp in Breakpoints) bp.Update();
        Breakpoints.RemoveAll(bp => bp.IsDeleted);
    }

    private async Task ExecuteRunCommand() {
        if (!Backend.Ready)
            return;
        if (IsBackgroundBusy)
            await StopEmulator();
        if (Backend.CurrentEmulator!.EffectivelyTerminated)
            Backend.MakeEmulator();

        Console.Clear();
        IsBackgroundBusy = true;
        State = ApplicationState.Running;
        OnDebuggerUpdating(false);

        var em = Backend.CurrentEmulator;
        Backend.TextInfoWriter.WriteLine("[EMULATOR] Running program.");
        var s = new Stopwatch();
        s.Start();

        while (!em.EffectivelyTerminated) {
            var last = s.ElapsedMilliseconds;
            await Task.Run(() => {
                // Keep running in parallel until ui thread is needed for console output.
                // Execute at least one instruction.
                do {
                    em.TryExecuteNext();
                } while (!em.EffectivelyTerminated &&
                         !ShouldUpdateConsole(s.ElapsedMilliseconds - last, em.ProgramCounter) &&
                         !em.ExpectingInput);
            });
            Console.FlushNewLines();


            // Await input or sth
            if (em.ExpectingInput) await AwaitInputAsync();
        }

        s.Stop();
        Backend.TextInfoWriter.WriteLine($"[EMULATOR] Program terminated after {s.ElapsedMilliseconds}ms");

        State = ApplicationState.Ready;
        OnDebuggerUpdating(true);
        Console.FlushNewLines();
        IsBackgroundBusy = false;
    }

    private async Task AwaitInputAsync() {
        var o = Observable.FromEventPattern<NotifyingTextWriterEventArgs>(
            a => Backend.TextInWriter.LineWritten += a,
            a => Backend.TextInWriter.LineWritten -= a);
        Console.IsExpectingInput = true;
        var t = o.FirstAsync().ToTask();
        while (!t.IsCompleted && !Backend.CurrentEmulator!.EffectivelyTerminated) {
            try {
                await t.WaitAsync(TimeSpan.FromMilliseconds(1000));
            }
            catch (TimeoutException) { }
        }
    }

    // Force pauses between console updates to keep UI responsive
    // except when there's a sysin coming up (otherwise prompts are not shown in time)
    private bool ShouldUpdateConsole(long msSinceUpdate, short pc)
        => Console.HasNewLines && (100 < msSinceUpdate ||
                                   (Backend.CurrentBuilder!.SymbolStacks.Length > pc &&
                                    Backend.CurrentEmulator!.Program[pc].Command == Keyword.Magic_Reg_Sysin));

    private async Task ExecuteDebugCommand() {
        if (!Backend.Ready)
            return;
        if (IsBackgroundBusy)
            await StopEmulator();
        Backend.MakeEmulator();


        Console.Clear();
        Backend.TextInfoWriter.WriteLine("[EMULATOR] Debug session started.");
        State = ApplicationState.Debugging;
        OnDebuggerBreaking();
        OnDebuggerUpdating(false);
        Console.FlushNewLines();
    }

    private async Task ExecuteStepCommand() {
        if (IsBackgroundBusy) return;
        var em = Backend.CurrentEmulator!;
        OnDebuggerBreakEnding();

        IsBackgroundBusy = true;
        var pc = em.ProgramCounter;
        while (!em.EffectivelyTerminated && pc == em.ProgramCounter) {
            await Task.Run(() => { em.TryExecuteNext(); });

            // Await input or sth
            if (em.ExpectingInput) await AwaitInputAsync();
        }

        if (em.Terminated || em.ErrorTerminated) {
            OnDebuggerBreakEnding();
            Backend.TextInfoWriter.WriteLine("[EMULATOR] Program terminated.");
            State = ApplicationState.Ready;
        }

        if (!em.EffectivelyTerminated) {
            OnDebuggerBreaking();
        }

        OnDebuggerUpdating(true);
        Console.FlushNewLines();
        IsBackgroundBusy = false;
    }

    private async Task ExecuteForwardCommand() {
        if (IsBackgroundBusy) return;

        var em = Backend.CurrentEmulator!;
        var s = new Stopwatch();
        s.Start();

        IsBackgroundBusy = true;
        while (!em.EffectivelyTerminated) {
            UpdateBreakpoints();
            var last = s.ElapsedMilliseconds;
            await Task.Run(() => {
                // Keep running in parallel until ui thread is needed for console output
                // Execute at least one instruction.
                do {
                    em.TryExecuteNext();
                } while (!em.EffectivelyTerminated &&
                         !ShouldUpdateConsole(s.ElapsedMilliseconds - last, em.ProgramCounter) &&
                         !IsAtBreakpoint(em.ProgramCounter) &&
                         !em.ExpectingInput);
            });
            Console.FlushNewLines();

            // Await input or sth
            if (em.ExpectingInput) await AwaitInputAsync();

            // Pause at breakpoints; but only after at least one instruction was executed.
            if (IsAtBreakpoint(em.ProgramCounter)) {
                OnDebuggerBreaking();
                break;
            }
        }

        s.Stop();

        if (em.Terminated || em.ErrorTerminated) {
            Backend.TextInfoWriter.WriteLine("[EMULATOR] Program terminated.");
            State = ApplicationState.Ready;
            OnDebuggerBreakEnding();
        }

        OnDebuggerUpdating(true);
        Console.FlushNewLines();
        IsBackgroundBusy = false;
    }

    private async Task ExecuteStopCommand() {
        if (Backend.CurrentEmulator!.EffectivelyTerminated) return;

        await StopEmulator();
        Backend.TextInfoWriter.WriteLine("[EMULATOR] Program terminated by user.");
        OnDebuggerUpdating(true);
        Console.FlushNewLines();
    }

    /// <summary>
    /// Force terminates the emulator. Needs to run in UI thread.
    /// </summary>
    public async Task StopEmulator() {
        Backend.CurrentEmulator!.ForceTerminated = true;

        if (IsBackgroundBusy) {
            var backgroundNoLongerBusy = this.WhenAnyValue(x => x.IsBackgroundBusy).FirstAsync();
            // Wait asynchronously to keep UI thread responsive.
            await Task.Run(() => backgroundNoLongerBusy.Wait());
        }

        if (IsDebugging) OnDebuggerBreakEnding();
        State = ApplicationState.Ready;
    }

    private bool IsAtBreakpoint(short pc) {
        return Backend.CurrentBuilder!.SymbolStacks.Length > pc &&
               Backend.CurrentBuilder!.SymbolStacks[pc].Any(
                   s => Breakpoints.Any(x => x.Line == s.Line));
    }

    public void UpdateCaretInfo(int positionLine, int positionColumn) {
        EditorCaretInfo = $"Pos {positionLine}:{positionColumn}";
    }

    protected virtual void OnDebuggerBreaking() {
        var line = Backend.CurrentBuilder!.SymbolStacks[Backend.CurrentEmulator!.ProgramCounter].Last().Line;
        DebuggerBreaking?.Invoke(this, new DebuggerBreakEventHandlerArgs(line));
    }

    protected virtual void OnDebuggerBreakEnding() {
        DebuggerBreakEnding?.Invoke(this, new DebuggerBreakEventHandlerArgs(-1));
    }

    protected virtual void OnRebuilt() {
        InstructionList.Clear();
        var prog = Backend.CurrentEmulator!.Program;
        for (var i = 0; i < prog.Count; i++) {
            var line = Backend.CurrentBuilder!.SymbolStacks[i].Last().Line;
            InstructionList.Add(new InstructionEntry(i, line, prog, _debuggerBreakChangingObservable));
        }

        Rebuilt?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnDebuggerUpdating(bool raisesChangeHighlight) {
        OverflowFlag = Backend.CurrentEmulator!.Registers.FlagOverflow ? 1 : 0;
        ProgramCounter = Backend.CurrentEmulator!.Registers.ProgramCounter;

        var index = 0;
        foreach (var address in Backend.CurrentEmulator!.Memory.Keys.Order()) {
            if (index >= MemoryList.Count)
                MemoryList.Add(new MemoryEntry(address, () => Backend.CurrentEmulator!.Memory,
                    _debuggerUpdatingObservable));
            else {
                while (MemoryList[index].AddressDecimal < address)
                    MemoryList.RemoveAt(index);
                if (MemoryList[index].AddressDecimal != address)
                    MemoryList.Insert(index, new MemoryEntry(address, () => Backend.CurrentEmulator!.Memory,
                        _debuggerUpdatingObservable));
            }

            index++;
        }

        while (index < MemoryList.Count) MemoryList.RemoveAt(index);

        DebuggerUpdating?.Invoke(this, new DebuggerUpdatingEventHandlerArgs(raisesChangeHighlight));
    }
}

public class DebuggerBreakEventHandlerArgs : EventArgs {
    public int Line { get; }

    public DebuggerBreakEventHandlerArgs(int line) {
        Line = line;
    }
}

public class DebuggerUpdatingEventHandlerArgs : EventArgs {
    /// <summary>
    /// Defines whether changes during this event should be highlighted.
    /// Should be false if update was not because of instruction execution.
    /// </summary>
    public bool RaisesChangeHighlight { get; }

    public DebuggerUpdatingEventHandlerArgs(bool raisesChangeHighlight) {
        RaisesChangeHighlight = raisesChangeHighlight;
    }
}