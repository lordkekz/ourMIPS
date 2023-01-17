﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
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

    public ReactiveCommand<Unit, Unit> MemInitCommand { get; }
    public ReactiveCommand<Unit, Unit> RebuildCommand { get; }
    public ReactiveCommand<Unit, Unit> DebugCommand { get; }
    public ReactiveCommand<Unit, Unit> RunCommand { get; }

    public TextDocument Document { get; }

    public MainViewModel() {
        MemInitCommand = ReactiveCommand.Create(() => throw new NotImplementedException());
        RebuildCommand = ReactiveCommand.CreateFromTask(ExecuteRebuildCommand);
        DebugCommand = ReactiveCommand.CreateFromTask(ExecuteDebugCommand);
        RunCommand = ReactiveCommand.CreateFromTask(ExecuteRunCommand);

        // Load mult_philos sample from unit tests
        Backend = new OpenScriptBackend(
            "../../../../../lib_ourMIPSSharp_Tests/Samples/instructiontests_philos.ourMIPS");
        Document = new TextDocument(Backend.SourceCode);
        Console = new ConsoleViewModel(Backend);
        UpdateData();
    }

    private async Task ExecuteRebuildCommand() {
        Console.Clear();
        var str = Document.Text;
        await Task.Run(() => {
            Backend.SourceCode = str;
            Backend.Rebuild();
        });
        UpdateData();
    }

    private async Task ExecuteDebugCommand() {
        await Task.Run(() => { Backend.CurrentEmulator.TryExecuteNext(); });
        UpdateData();
    }

    private async Task ExecuteRunCommand() {
        if (!Backend.Ready)
            return;
        if (Backend.CurrentEmulator!.EffectivelyTerminated)
            Backend.MakeEmulator();

        Console.Clear();

        var em = Backend.CurrentEmulator;
        Backend.TextInfoWriter.WriteLine("[EMULATOR] Running program.");
        var s = new Stopwatch();
        s.Start();

        while (!em.EffectivelyTerminated) {
            var last = s.ElapsedMilliseconds;
            await Task.Run(() => {
                // Keep running in parallel until ui thread is needed for console output
                while (!em.EffectivelyTerminated &&
                       !ShouldUpdateConsole(s.ElapsedMilliseconds - last, em.Program[em.ProgramCounter].Command)) {
                    em.TryExecuteNext();
                }
            });
            Console.FlushNewLines();
        }

        s.Stop();
        Backend.TextInfoWriter.WriteLine($"[EMULATOR] Program terminated after {s.ElapsedMilliseconds}ms");

        UpdateData();
    }

    // Force pauses between console updates to keep UI responsive
    // except when there's a sysin coming up (otherwise prompts are not shown in time)
    private bool ShouldUpdateConsole(long msSinceUpdate, Keyword nextCommand)
        => Console.HasNewLines && (100 < msSinceUpdate || nextCommand == Keyword.Magic_Reg_Sysin);

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