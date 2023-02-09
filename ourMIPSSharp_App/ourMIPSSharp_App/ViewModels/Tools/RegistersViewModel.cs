using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using Dock.Model.ReactiveUI.Controls;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.EmulatorComponents;
using ourMIPSSharp_App.Models;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels.Tools;

public class RegistersViewModel : Tool {
    private readonly ObservableAsPropertyHelper<ObservableCollection<RegisterEntry>?> _entries;
    private readonly ObservableAsPropertyHelper<int> _programCounter;
    private readonly ObservableAsPropertyHelper<int> _overflowFlag;
    public ObservableCollection<RegisterEntry>? Entries => _entries.Value;
    public int ProgramCounter => _programCounter.Value;
    public int OverflowFlag => _overflowFlag.Value;

    public RegistersViewModel(MainViewModel main) {
        Id = "Registers";
        Title = "Registers";
        main.WhenAnyValue(m => m.DebugSession!.RegisterList)
            .ToProperty(this, x => x.Entries, out _entries);
        main.WhenAnyValue(m => m.DebugSession!.ProgramCounter)
            .ToProperty(this, x => x.ProgramCounter, out _programCounter);
        main.WhenAnyValue(m => m.DebugSession!.OverflowFlag)
            .ToProperty(this, x => x.OverflowFlag, out _overflowFlag);
    }
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