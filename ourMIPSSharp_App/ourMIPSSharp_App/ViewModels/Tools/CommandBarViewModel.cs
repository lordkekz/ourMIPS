using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels.Tools;

public class CommandBarViewModel : ViewModelBase {
    #region Properties

    public ReactiveCommand<Unit, Unit> SettingsCommand { get; }
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
        var isDebuggingButNotBusy = Main.WhenAnyValue(x => x.State,
                x => x.CurrentFile.IsBackgroundBusy)
            .Select(t => t is { Item1: ApplicationState.Debugging, Item2: false });
        var isBuiltButEmulatorInactive = Main.WhenAnyValue(x => x.State, s => s.IsBuilt() && !s.IsEmulatorActive());

        SettingsCommand = ReactiveCommand.Create(ExecuteSettingsCommand);
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

    private async Task ExecuteFileOpenCommand() {
        try {
            var file = await Interactions.OpenProgramFile.Handle(Unit.Default);
            if (file is null) return;
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var sourceCode = await reader.ReadToEndAsync();

            Main.OpenProgramFromSource(sourceCode);
        }
        catch (IOException ex) {
            Console.Error.WriteLine(ex);
        }
    }

    private void ExecuteSettingsCommand() {
        Main.IsSettingsOpened = !Main.IsSettingsOpened;
    }

    private async Task ExecuteRebuildCommand() {
        if (Main.CurrentFile is null || Main.CurrentFile!.IsBackgroundBusy) return;
        Main.CurrentConsole?.Clear();
        Main.CurrentFile.State = ApplicationState.Rebuilding;
        var str = Main.CurrentEditor!.Text;
        await Task.Run(() => {
            Main.CurrentBackend!.SourceCode = str;
            Main.CurrentBackend.Rebuild();
            Main.CurrentBackend.MakeEmulator();
        });

        await Main.CurrentConsole!.FlushNewLines();
        if (Main.CurrentBackend!.Ready) {
            Main.CurrentFile.State = ApplicationState.Built;
            Main.CurrentEditor.OnRebuilt(Main.CurrentEditor!.DebuggerBreakChangingObservable);
        }
        else Main.CurrentFile.State = ApplicationState.FileOpened;
    }

    private async Task ExecuteRunCommand() {
        if (Main.CurrentFile is null || Main.CurrentFile!.IsBackgroundBusy || !Main.CurrentBackend!.Ready) return;
        Main.CurrentFile.IsBackgroundBusy = true;

        Main.CurrentBackend.MakeEmulator();
        Main.CurrentConsole!.Clear();
        Main.CurrentFile.State = ApplicationState.Running;
        await Task.Run(Main.CurrentEditor!.DebuggerInstance.Run);
        Main.CurrentFile.State = ApplicationState.Built;
        await Main.CurrentConsole!.FlushNewLines();
        Main.CurrentFile.IsBackgroundBusy = false;
    }

    private async Task ExecuteDebugCommand() {
        if (Main.CurrentFile is null || Main.CurrentFile!.IsBackgroundBusy || !Main.CurrentBackend!.Ready) return;
        Main.CurrentBackend.MakeEmulator();

        Main.CurrentConsole!.Clear();
        Main.CurrentEditor!.DebuggerInstance.StartSession();
        Main.CurrentFile.State = ApplicationState.Debugging;
        await Main.CurrentConsole!.FlushNewLines();
    }

    private async Task ExecuteStepCommand() {
        if (Main.CurrentFile is null || Main.CurrentFile!.IsBackgroundBusy) return;
        Main.CurrentFile.IsBackgroundBusy = true;

        Main.CurrentFile.State = ApplicationState.Running;
        await Task.Run(Main.CurrentEditor!.DebuggerInstance.Step);

        if (Main.CurrentBackend!.CurrentEmulator!.Terminated || Main.CurrentBackend.CurrentEmulator!.ErrorTerminated)
            Main.CurrentFile.State = ApplicationState.Built;
        else
            Main.CurrentFile.State = ApplicationState.Debugging;
        await Main.CurrentConsole!.FlushNewLines();
        Main.CurrentFile.IsBackgroundBusy = false;
    }

    private async Task ExecuteForwardCommand() {
        if (Main.CurrentFile is null || Main.CurrentFile!.IsBackgroundBusy) return;
        Main.CurrentFile.IsBackgroundBusy = true;

        Main.CurrentFile.State = ApplicationState.Running;
        await Task.Run(Main.CurrentEditor!.DebuggerInstance.Forward);

        if (Main.CurrentBackend!.CurrentEmulator!.Terminated || Main.CurrentBackend.CurrentEmulator!.ErrorTerminated)
            Main.CurrentFile.State = ApplicationState.Built;
        else if (!Main.CurrentBackend.CurrentEmulator!.ForceTerminated)
            Main.CurrentFile.State = ApplicationState.Debugging;
        await Main.CurrentConsole!.FlushNewLines();
        Main.CurrentFile.IsBackgroundBusy = false;
    }

    private async Task ExecuteStopCommand() {
        if (Main.CurrentBackend?.CurrentEmulator is null ||
            Main.CurrentBackend.CurrentEmulator.EffectivelyTerminated) return;

        await Main.CurrentEditor!.StopEmulator();
        Main.CurrentFile!.State = ApplicationState.Built;
        await Main.CurrentConsole!.FlushNewLines();
    }

    #endregion
}