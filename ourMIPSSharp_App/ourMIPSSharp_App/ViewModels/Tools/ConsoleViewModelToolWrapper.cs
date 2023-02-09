using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Dock.Model.ReactiveUI.Controls;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels.Tools;

public class ConsoleViewModelToolWrapper : Tool {
    private readonly ObservableAsPropertyHelper<ConsoleViewModel?> _activeConsole;
    private readonly ObservableAsPropertyHelper<bool> _hasActiveConsole;

    public ConsoleViewModel? ActiveConsole => _activeConsole.Value;
    public bool HasActiveConsole => _hasActiveConsole.Value;
    public MainViewModel Main { get; }

    public ConsoleViewModelToolWrapper(MainViewModel main) {
        Main = main;
        Id = Title = "Console";
        CanClose = CanFloat = CanPin = false;
        
        main.WhenAnyValue(x => x.DebugSession!.Editor.DebugConsole)
            .ToProperty(this, x => x.ActiveConsole, out _activeConsole);
        this.WhenAnyValue(x => x.ActiveConsole)
            .Select(c => c is not null)
            .ToProperty(this, x => x.HasActiveConsole, out _hasActiveConsole);
    }

    public void SubmitInput() => ActiveConsole?.SubmitInput();
    public void Clear() => ActiveConsole?.Clear();
    public void DoFlushNewLines() => ActiveConsole?.DoFlushNewLines();

    public async Task FlushNewLines() {
        if (ActiveConsole is not null) await ActiveConsole.FlushNewLines();
    }
}