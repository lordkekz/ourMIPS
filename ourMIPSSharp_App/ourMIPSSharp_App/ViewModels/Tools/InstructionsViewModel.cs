using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Dock.Model.ReactiveUI.Controls;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.EmulatorComponents;
using ourMIPSSharp_App.Models;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels.Tools;

public class InstructionsViewModel : Tool {
    
    private readonly ObservableAsPropertyHelper<ObservableCollection<InstructionEntry>?> _entries;
    public ObservableCollection<InstructionEntry>? Entries => _entries.Value;

    public InstructionsViewModel(MainViewModel main) {
        Id = "Instructions";
        Title = "Instructions";
        main.WhenAnyValue(m => m.CurrentFile)
            .Select(f => f?.Editor.InstructionList)
            .ToProperty(this, x => x.Entries, out _entries);
    }
}

public class InstructionEntry : ViewModelBase {
    private int _address;
    private int _line;
    private ProgramStorage _program;


    public InstructionEntry(int address, int line, ProgramStorage program,
        IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> breakingObservable) {
        _address = address;
        _line = line;
        _program = program;
        breakingObservable.Select(x => x.EventArgs.Address == _address)
            .ToProperty(this, x => x.IsCurrent, out _isCurrent);
    }

    public bool IsCurrent => _isCurrent.Value;
    private ObservableAsPropertyHelper<bool> _isCurrent;

    public int LineNumber => _line;
    public int AddressDecimal => _address;
    public string AddressHex => _address.ToString(NumberLiteralFormat.HexPrefix);
    public string Bytecode => Convert.ToString(_program[_address].Bytecode, 2).PadLeft(32, '0');
    public string InstructionString => _program[_address].ToString();
}