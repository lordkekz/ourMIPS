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

public class MemoryViewModel : Tool {
    
    private readonly ObservableAsPropertyHelper<ObservableCollection<MemoryEntry>?> _entries;
    public ObservableCollection<MemoryEntry>? Entries => _entries.Value;

    public MemoryViewModel(MainViewModel main) {
        Id = "Memory";
        Title = "Memory";
        main.WhenAnyValue(m => m.CurrentFile)
            .Select(f => f?.Editor.MemoryList)
            .ToProperty(this, x => x.Entries, out _entries);
    }
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