using System;
using System.Reactive;
using System.Reactive.Linq;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.EmulatorComponents;
using ourMIPSSharp_App.Models;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

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

public class RegisterEntry : ViewModelBase {
    private readonly Register _register;
    private readonly Func<RegisterStorage?> _registerStorageFunc;
    private int? _last;

    public RegisterEntry(Register register, Func<RegisterStorage?> registerStorageFunc,
        IObservable<EventPattern<DebuggerUpdatingEventHandlerArgs>> updatingObservable) {
        _register = register;
        _registerStorageFunc = registerStorageFunc;
        int? tmpLast = null;
        updatingObservable
            .Do(x => tmpLast = _last)
            .Do(x => _last = _registerStorageFunc()?[_register])
            .Select(x =>
                x.EventArgs.RaisesChangeHighlight && tmpLast.HasValue &&
                _registerStorageFunc()?[_register] != tmpLast.Value)
            .ToProperty(this, x => x.HasChanged, out _hasChanged);
        updatingObservable.Select(x => _registerStorageFunc()?[_register].ToString(NumberLiteralFormat.Decimal))
            .ToProperty(this, x => x.ValueDecimal, out _valueDecimal);
        updatingObservable.Select(x => _registerStorageFunc()?[_register].ToString(NumberLiteralFormat.HexPrefix))
            .ToProperty(this, x => x.ValueHex, out _valueHex);
        updatingObservable.Select(x => _registerStorageFunc()?[_register].ToString(NumberLiteralFormat.BinaryPrefix))
            .ToProperty(this, x => x.ValueBinary, out _valueBinary);
    }

    public int RegId => (int)_register;
    public string Name => _register.ToString().ToLower();

    public bool HasChanged => _hasChanged.Value;
    private readonly ObservableAsPropertyHelper<bool> _hasChanged;
    public string? ValueDecimal => _valueDecimal.Value;
    private readonly ObservableAsPropertyHelper<string?> _valueDecimal;
    public string? ValueHex => _valueHex.Value;
    private readonly ObservableAsPropertyHelper<string?> _valueHex;
    public string? ValueBinary => _valueBinary.Value;
    private readonly ObservableAsPropertyHelper<string?> _valueBinary;
}

public class MemoryEntry : ViewModelBase {
    private Func<MainStorage> _ramFunc;
    private int? _last;

    public MemoryEntry(int address, Func<MainStorage> memory,
        IObservable<EventPattern<DebuggerUpdatingEventHandlerArgs>> updatingObservable) {
        AddressDecimal = address;
        _ramFunc = memory;
        int? tmpLast = null;
        updatingObservable
            .Do(x => tmpLast = _last)
            .Do(x => _last = _ramFunc()[AddressDecimal])
            .Select(x =>
                x.EventArgs.RaisesChangeHighlight && tmpLast.HasValue && _ramFunc()[AddressDecimal] != tmpLast.Value)
            .ToProperty(this, x => x.HasChanged, out _hasChanged);
        updatingObservable.Select(x => _ramFunc()[AddressDecimal].ToString(NumberLiteralFormat.Decimal))
            .ToProperty(this, x => x.ValueDecimal, out _valueDecimal);
        updatingObservable.Select(x => _ramFunc()[AddressDecimal].ToString(NumberLiteralFormat.HexPrefix))
            .ToProperty(this, x => x.ValueHex, out _valueHex);
        updatingObservable.Select(x => _ramFunc()[AddressDecimal].ToString(NumberLiteralFormat.BinaryPrefix))
            .ToProperty(this, x => x.ValueBinary, out _valueBinary);
    }

    public bool HasChanged => _hasChanged.Value;
    private ObservableAsPropertyHelper<bool> _hasChanged;
    public int AddressDecimal { get; }
    public string AddressHex => AddressDecimal.ToString(NumberLiteralFormat.HexPrefix);
    public string ValueDecimal => _valueDecimal.Value;
    private ObservableAsPropertyHelper<string> _valueDecimal;
    public string ValueHex => _valueHex.Value;
    private ObservableAsPropertyHelper<string> _valueHex;
    public string ValueBinary => _valueBinary.Value;
    private ObservableAsPropertyHelper<string> _valueBinary;
}