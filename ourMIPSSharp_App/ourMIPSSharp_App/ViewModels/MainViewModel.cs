using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media.Transformation;
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

    private string _consoleOut = "";

    public string ConsoleOut {
        get => _consoleOut;
        set => this.RaiseAndSetIfChanged(ref _consoleOut, value);
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
        UpdateData();
    }

    private async Task ExecuteRebuildCommand() {
        var str = Document.Text;
        await Task.Run(() => {
            Backend.Rebuild(str);
        });
        UpdateData();
    }

    private async Task ExecuteDebugCommand() {
        await Task.Run(() => {
            Backend.CurrentEmulator.TryExecuteNext();
        });
        UpdateData();
    }

    private async Task ExecuteRunCommand() {
        await Task.Run(() => {
            Backend.CurrentEmulator.RunUntilTerminated();
        });
        UpdateData();
    }

    public void UpdateData() {
        ConsoleOut = Backend.TextOutWriter.ToString();

        if (!Backend.Ready)
            return;

        OverflowFlag = Backend.CurrentEmulator!.Registers.FlagOverflow ? 1 : 0;
        ProgramCounter = Backend.CurrentEmulator!.Registers.ProgramCounter;

        InstructionList.Clear();
        var prog = Backend.CurrentEmulator!.Program;
        for (var i = 0; i < prog.Count; i++) {
            InstructionList.Add(new InstructionEntry(i, prog));
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