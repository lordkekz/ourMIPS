namespace ourMIPSSharp_App.ViewModels; 

public enum ApplicationState {
    None,
    FileOpened,
    Built,
    Rebuilding,
    Running,
    DebugBreak,
    DebugRunning
}

public static class ApplicationStateExtensions {
    public static bool IsBuilt(this ApplicationState state) =>
        state is ApplicationState.Built or ApplicationState.Running or ApplicationState.DebugBreak;
    public static bool IsEmulatorActive(this ApplicationState state) =>
        state is ApplicationState.Running or ApplicationState.DebugBreak or ApplicationState.DebugRunning;
    public static bool IsDebuggerActive(this ApplicationState state) =>
        state is ApplicationState.DebugBreak or ApplicationState.DebugRunning;
    public static bool IsRebuildingAllowed(this ApplicationState state) =>
        state is ApplicationState.FileOpened or ApplicationState.Built;
}