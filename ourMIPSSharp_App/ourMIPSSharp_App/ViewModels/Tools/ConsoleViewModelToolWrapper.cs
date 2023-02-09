using System;
using System.Threading.Tasks;
using Dock.Model.ReactiveUI.Controls;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels.Tools;

public class ConsoleViewModelToolWrapper : Tool {
    private readonly ObservableAsPropertyHelper<ConsoleViewModel?> _activeConsole;

    public ConsoleViewModel? ActiveConsole => _activeConsole.Value;
    public MainViewModel Main { get; }

    public ConsoleViewModelToolWrapper(MainViewModel main) {
        Main = main;
        Id = Title = "Console";
        main.WhenAnyValue(x => x.DebugSession!.Editor.DebugConsole)
            .ToProperty(this, x => x.ActiveConsole, out _activeConsole);
    }

    public void SubmitInput() => ActiveConsole?.SubmitInput();
    public void Clear() => ActiveConsole?.Clear();
    public void DoFlushNewLines() => ActiveConsole?.DoFlushNewLines();

    public async Task FlushNewLines() {
        if (ActiveConsole is not null) await ActiveConsole.FlushNewLines();
    }
}