using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using AvaloniaEdit.Document;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.EmulatorComponents;
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

    private ConsoleViewModel _console;

    public ConsoleViewModel Console {
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

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> MemInitCommand { get; }
    public ReactiveCommand<Unit, Unit> RebuildCommand { get; }
    public ReactiveCommand<Unit, Unit> RunCommand { get; }
    public ReactiveCommand<Unit, Unit> DebugCommand { get; }
    public ReactiveCommand<Unit, Unit> StepCommand { get; }
    public ReactiveCommand<Unit, Unit> ForwardCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    public TextDocument Document { get; }

    private ApplicationState _state = ApplicationState.Started;

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

    public MainViewModel() {
        var canExecuteNever = new[] { false }.ToObservable();
        var isRebuildingAllowed = this.WhenAnyValue(x => x.State,
            s => s.IsRebuildingAllowed());
        var isEmulatorActive = this.WhenAnyValue(x => x.State,
            s => s.IsEmulatorActive());
        var isDebuggingButNotBusy = this.WhenAnyValue(x => x.State,
                x => x.IsBackgroundBusy)
            .Select(t => t is { Item1: ApplicationState.Debugging, Item2: false });
        var isBuilt = this.WhenAnyValue(x => x.State, s => s.IsBuilt());
        SaveCommand = ReactiveCommand.Create(() => throw new NotImplementedException(), canExecuteNever);
        MemInitCommand = ReactiveCommand.Create(() => throw new NotImplementedException(), canExecuteNever);
        RebuildCommand = ReactiveCommand.CreateFromTask(ExecuteRebuildCommand, isRebuildingAllowed);
        RunCommand = ReactiveCommand.CreateFromTask(ExecuteRunCommand, isBuilt);
        DebugCommand = ReactiveCommand.CreateFromTask(ExecuteDebugCommand, isBuilt);
        StepCommand = ReactiveCommand.CreateFromTask(ExecuteStepCommand, isDebuggingButNotBusy);
        ForwardCommand = ReactiveCommand.CreateFromTask(ExecuteForwardCommand, isDebuggingButNotBusy);
        StopCommand = ReactiveCommand.CreateFromTask(ExecuteStopCommand, isEmulatorActive);

        this.WhenAnyValue(x => x.State)
            .Select(x => !x.IsEditingAllowed())
            .ToProperty(this, x => x.IsEditorReadonly, out _isEditorReadonly);

        this.WhenAnyValue(x => x.State, s => s == ApplicationState.Debugging)
            .ToProperty(this, x => x.IsDebugging, out _isDebugging);
        
        isEmulatorActive.ToProperty(this, x => x.IsEmulatorActive, out _isEmulatorActive);

        // Load mult_philos sample from unit tests
        Backend = new OpenScriptBackend(
            "../../../../../lib_ourMIPSSharp_Tests/Samples/instructiontests_philos.ourMIPS");
        Document = new TextDocument(Backend.SourceCode);
        Console = new ConsoleViewModel(Backend);
        UpdateData();
    }

    private async Task ExecuteRebuildCommand() {
        Console.Clear();
        State = ApplicationState.Rebuilding;
        var str = Document.Text;
        await Task.Run(() => {
            Backend.SourceCode = str;
            Backend.Rebuild();
        });

        State = ApplicationState.Ready;
        UpdateData();
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
        State = ApplicationState.Running;

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
                         !ShouldUpdateConsole(s.ElapsedMilliseconds - last, em.ProgramCounter));
            });
            Console.FlushNewLines();
        }

        s.Stop();
        Backend.TextInfoWriter.WriteLine($"[EMULATOR] Program terminated after {s.ElapsedMilliseconds}ms");

        State = ApplicationState.Ready;
        UpdateData();
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
        UpdateData();
    }

    private async Task ExecuteStepCommand() {
        if (IsBackgroundBusy) return;
        var em = Backend.CurrentEmulator!;

        IsBackgroundBusy = true;
        await Task.Run(() => { em.TryExecuteNext(); });
        IsBackgroundBusy = false;

        if (em.Terminated || em.ErrorTerminated) {
            Backend.TextInfoWriter.WriteLine("[EMULATOR] Program terminated.");
            State = ApplicationState.Ready;
        }

        UpdateData();
    }

    private async Task ExecuteForwardCommand() {
        if (IsBackgroundBusy) return;

        var em = Backend.CurrentEmulator;
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
                         !IsAtBreakpoint(em.ProgramCounter));
            });
            Console.FlushNewLines();

            // Pause at breakpoints; but only after at least one instruction was executed.
            if (IsAtBreakpoint(em.ProgramCounter)) break;
        }

        IsBackgroundBusy = false;
        s.Stop();

        if (em.Terminated || em.ErrorTerminated) {
            Backend.TextInfoWriter.WriteLine("[EMULATOR] Program terminated.");
            State = ApplicationState.Ready;
        }

        UpdateData();
    }

    private async Task ExecuteStopCommand() {
        if (Backend.CurrentEmulator!.EffectivelyTerminated) return;

        await StopEmulator();
        Backend.TextInfoWriter.WriteLine("[EMULATOR] Program terminated by user.");
        UpdateData();
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

        State = ApplicationState.Ready;
    }

    private bool IsAtBreakpoint(short pc) {
        return Backend.CurrentBuilder!.SymbolStacks.Length > pc &&
               Backend.CurrentBuilder!.SymbolStacks[pc].Any(
                   s => Breakpoints.Any(x => x.Line == s.Line));
    }

    public void UpdateData() {
        Console.FlushNewLines();
        if (!Backend.Ready)
            return;

        OverflowFlag = Backend.CurrentEmulator!.Registers.FlagOverflow ? 1 : 0;
        ProgramCounter = Backend.CurrentEmulator!.Registers.ProgramCounter;

        InstructionList.Clear();
        var prog = Backend.CurrentEmulator!.Program;
        for (var i = 0; i < prog.Count; i++) {
            var line = Backend.CurrentBuilder!.SymbolStacks[i].Last().Line;
            InstructionList.Add(new InstructionEntry(i, line, prog));
        }

        RegisterList.Clear();
        var regs = Backend.CurrentEmulator!.Registers;
        for (var i = 0; i < 32; i++) {
            RegisterList.Add(new RegisterEntry((Register)i, regs));
        }

        MemoryList.Clear();
        var memory = Backend.CurrentEmulator!.Memory;
        foreach (var address in memory.Keys.Order()) {
            MemoryList.Add(new MemoryEntry(address, memory));
        }
    }
}