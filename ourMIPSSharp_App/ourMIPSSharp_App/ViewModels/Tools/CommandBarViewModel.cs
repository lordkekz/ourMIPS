using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using ourMIPSSharp_App.Models;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels.Tools;

public class CommandBarViewModel : ViewModelBase {
    #region Properties

    public ReactiveCommand<Unit, Unit> SettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateDocumentCommand { get; }
    public ReactiveCommand<Unit, Unit> FileOpenCommand { get; }
    public ReactiveCommand<Unit, Unit> MemInitCommand { get; }
    public ReactiveCommand<Unit, Unit> RebuildCommand { get; }
    public ReactiveCommand<Unit, Unit> RunCommand { get; }
    public ReactiveCommand<Unit, Unit> DebugCommand { get; }
    public ReactiveCommand<Unit, Unit> StepCommand { get; }
    public ReactiveCommand<Unit, Unit> ForwardCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    public MainViewModel Main { get; }

    #endregion

    public CommandBarViewModel(MainViewModel main) {
        Main = main;

        var canExecuteNever = new[] { false }.ToObservable();
        var canExecuteAlways = new[] { true }.ToObservable();

        var isRebuildingAllowed = Main.WhenAnyValue(x => x.State,
            s => s.IsRebuildingAllowed());
        var isEmulatorActive = Main.WhenAnyValue(x => x.State,
            s => s.IsEmulatorActive());
        var isDebuggingButNotBusy = Main.WhenAnyValue(x => x.State, s => s == ApplicationState.DebugBreak);
        var isBuiltButEmulatorInactive = Main.WhenAnyValue(x => x.State, s => s.IsBuilt() && !s.IsEmulatorActive());

        SettingsCommand = ReactiveCommand.Create(ExecuteSettingsCommand);
        CreateDocumentCommand = ReactiveCommand.CreateFromTask(ExecuteCreateDocumentCommand);
        FileOpenCommand = ReactiveCommand.CreateFromTask(ExecuteFileOpenCommand);
        MemInitCommand = ReactiveCommand.Create(() => throw new NotImplementedException(), canExecuteNever);
        RebuildCommand = ReactiveCommand.CreateFromTask(ExecuteRebuildCommand, isRebuildingAllowed);
        RunCommand = ReactiveCommand.CreateFromTask(ExecuteRunCommand, isBuiltButEmulatorInactive);
        DebugCommand = ReactiveCommand.CreateFromTask(ExecuteDebugCommand, isBuiltButEmulatorInactive);
        StepCommand = ReactiveCommand.CreateFromTask(ExecuteStepCommand, isDebuggingButNotBusy);
        ForwardCommand = ReactiveCommand.CreateFromTask(ExecuteForwardCommand, isDebuggingButNotBusy);
        StopCommand = ReactiveCommand.CreateFromTask(ExecuteStopCommand, isEmulatorActive);
    }

    #region ExecuteCommandMethods

    private void ExecuteSettingsCommand() {
        Main.IsSettingsOpened = !Main.IsSettingsOpened;
    }

    private async Task ExecuteCreateDocumentCommand() {
        await Main.OpenProgramFromSourceAsync("# New Program\n\n");
    }

    private async Task ExecuteFileOpenCommand() {
        try {
            var file = await Interactions.OpenProgramFile.Handle(Unit.Default);
            if (file is null) return;
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var sourceCode = await reader.ReadToEndAsync();

            await Main.OpenProgramFromSourceAsync(sourceCode);
            Main.CurrentFile.Name = file.Name;
        }
        catch (IOException ex) {
            Console.Error.WriteLine(ex);
        }
    }

    private async Task ExecuteRebuildCommand() {
        var f = Main.CurrentFile;
        if (f is null || Main.IsEmulatorActive || Main.State == ApplicationState.Rebuilding) return;
        Main.State = ApplicationState.Rebuilding;
        f.DebugConsole.Clear();
        var str = f.Text;
        await Task.Run(() => {
            f.Backend.SourceCode = str;
            f.Backend.Rebuild(Main.Settings?.SelectedCompilerMode.ToDialectOptions() ?? DialectOptions.None);
            if (f.Backend.Ready) f.Backend.MakeEmulator();
        });

        f.DebugConsole.DoFlushNewLines();
        Main.DebugSession = f.DebugSession;
        if (f.Backend.Ready) {
            Main.State = ApplicationState.Built;
            f.DebugDocument.Text = str;
            f.OnRebuilt(f.DebuggerBreakChangingObservable);
        }
        else Main.State = ApplicationState.FileOpened;
    }

    private async Task ExecuteRunCommand() {
        var s = Main.DebugSession;
        if (s is null || !s.Backend.Ready || s.IsBackgroundBusy) return;

        s.IsBackgroundBusy = true;
        s.Editor.DebugConsole.Clear();

        s.Backend.MakeEmulator();
        Main.State = ApplicationState.Running;
        await Task.Run(s.DebuggerInstance.Run);
        Main.State = ApplicationState.Built;

        s.Editor.DebugConsole.DoFlushNewLines();
        s.IsBackgroundBusy = false;
    }

    private async Task ExecuteDebugCommand() {
        var s = Main.DebugSession;
        if (s is null || !s.Backend.Ready || s.IsBackgroundBusy) return;
        s.Backend.MakeEmulator();

        s.Editor.DebugConsole.Clear();
        s.DebuggerInstance.StartSession();
        Main.State = ApplicationState.DebugBreak;
        s.Editor.DebugConsole.DoFlushNewLines();
    }

    private async Task ExecuteStepCommand() {
        var s = Main.DebugSession;
        if (s is null || !s.Backend.Ready || s.IsBackgroundBusy) return;

        s.IsBackgroundBusy = true;
        Main.State = ApplicationState.DebugRunning;
        await Task.Run(s.DebuggerInstance.Step);

        if (s.Backend.CurrentEmulator!.Terminated || s.Backend.CurrentEmulator!.ErrorTerminated)
            Main.State = ApplicationState.Built;
        else
            Main.State = ApplicationState.DebugBreak;

        s.Editor.DebugConsole.DoFlushNewLines();
        s.IsBackgroundBusy = false;
    }

    private async Task ExecuteForwardCommand() {
        var s = Main.DebugSession;
        if (s is null || !s.Backend.Ready || s.IsBackgroundBusy) return;

        s.IsBackgroundBusy = true;
        Main.State = ApplicationState.DebugRunning;
        await Task.Run(s.DebuggerInstance.Forward);

        if (s.Backend.CurrentEmulator!.Terminated || s.Backend.CurrentEmulator!.ErrorTerminated)
            Main.State = ApplicationState.Built;
        else if (!s.Backend.CurrentEmulator!.ForceTerminated)
            Main.State = ApplicationState.DebugBreak;
        s.Editor.DebugConsole.DoFlushNewLines();
        s.IsBackgroundBusy = false;
    }

    private async Task ExecuteStopCommand() {
        var s = Main.DebugSession;
        if (s is null || !s.Backend.Ready) return;

        await s.StopEmulator();
        Main.State = ApplicationState.Built;
        s.Editor.DebugConsole.DoFlushNewLines();
    }

    #endregion
}